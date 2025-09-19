using DevOpsGuard.Domain.Enums;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsGuard.Api.Services;

public sealed class DailyMetricsSnapshotService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DailyMetricsSnapshotService> _logger;
    private readonly IHostEnvironment _env;
    private readonly bool _useSql;
    private readonly bool _auto;
    private readonly int _hourLocal;

    public DailyMetricsSnapshotService(
        IServiceProvider sp,
        ILogger<DailyMetricsSnapshotService> logger,
        IHostEnvironment env,
        IConfiguration config)
    {
        _sp = sp;
        _logger = logger;
        _env = env;

        _useSql   = config.GetValue("UseSqlServer", false);
        _auto     = config.GetValue("Metrics:AutoCapture", false);
        _hourLocal= Math.Clamp(config.GetValue("Metrics:SnapshotHourLocal", 9), 0, 23);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_useSql || !_auto)
        {
            _logger.LogInformation("Daily snapshot disabled (UseSqlServer={UseSql}, AutoCapture={Auto})", _useSql, _auto);
            return;
        }

        // loop forever until shutdown
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowLocal = DateTime.Now; // local clock
                var todayRun = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, _hourLocal, 0, 0, DateTimeKind.Local);

                // If we've passed today's run time, schedule tomorrow; else, schedule today
                var nextRunLocal = nowLocal <= todayRun ? todayRun : todayRun.AddDays(1);
                var delay = nextRunLocal - nowLocal;

                _logger.LogInformation("Daily snapshot scheduled for {NextRun}", nextRunLocal);
                await Task.Delay(delay, stoppingToken);

                // do the work (in a scope so we get a fresh DbContext)
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DevOpsGuardDbContext>();

                var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

                // if we already have a snapshot today, skip
                var already = await db.MetricsSnapshots.AsNoTracking()
                    .AnyAsync(s => DateOnly.FromDateTime(s.CapturedAtUtc) == todayUtc, stoppingToken);

                if (already)
                {
                    _logger.LogInformation("Snapshot already exists for {Day}, skipping.", todayUtc);
                }
                else
                {
                    await CaptureAsync(db, stoppingToken);
                    _logger.LogInformation("Snapshot captured at {NowUtc}.", DateTime.UtcNow);
                }
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Snapshot capture failed.");
                // wait a bit before retrying scheduling to avoid tight error loop
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private static async Task CaptureAsync(DevOpsGuardDbContext db, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var openItemsQ = db.WorkItems.AsNoTracking()
            .Where(w => w.Status != WorkItemStatus.Resolved);

        var openCount = await openItemsQ.CountAsync(ct);
        var touchedRecently = await openItemsQ.CountAsync(w => w.UpdatedAtUtc >= sevenDaysAgo, ct);
        var overdueOpen = await openItemsQ.CountAsync(w => w.DueDate != null && w.DueDate < today, ct);

        var backlogHealthPct = openCount == 0 ? 100.0 : Math.Round(100.0 * touchedRecently / openCount, 1);
        var slaBreachRatePct = openCount == 0 ? 0.0 : Math.Round(100.0 * overdueOpen / openCount, 1);

        var comps = await openItemsQ.Select(w => new { w.Priority, w.DueDate }).ToListAsync(ct);
        double riskAvg = 0.0;
        if (comps.Count > 0)
        {
            double sum = 0;
            foreach (var x in comps)
            {
                var baseScore = x.Priority switch
                {
                    Priority.Low    => 10,
                    Priority.Medium => 25,
                    Priority.High   => 50,
                    Priority.P0     => 70,
                    _               => 25
                };
                var daysOverdue = x.DueDate.HasValue ? Math.Max(0, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - x.DueDate.Value.DayNumber) : 0;
                sum += Math.Clamp(baseScore + 3 * daysOverdue, 0, 100);
            }
            riskAvg = Math.Round(sum / comps.Count, 1);
        }

        var snap = new MetricsSnapshot(
            capturedAtUtc: DateTime.UtcNow,
            backlogHealthPct: backlogHealthPct,
            slaBreachRatePct: slaBreachRatePct,
            overdueCount: overdueOpen,
            riskAvg: riskAvg
        );

        db.MetricsSnapshots.Add(snap);
        await db.SaveChangesAsync(ct);
    }
}
