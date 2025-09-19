namespace DevOpsGuard.Domain.Entities;

public sealed class MetricsSnapshot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CapturedAtUtc { get; private set; }

    public double BacklogHealthPct { get; private set; }
    public double SlaBreachRatePct { get; private set; }
    public int OverdueCount { get; private set; }
    public double RiskAvg { get; private set; }

    // EF constructor
    private MetricsSnapshot() { }

    public MetricsSnapshot(DateTime capturedAtUtc, double backlogHealthPct, double slaBreachRatePct, int overdueCount, double riskAvg)
    {
        CapturedAtUtc = capturedAtUtc;
        BacklogHealthPct = backlogHealthPct;
        SlaBreachRatePct = slaBreachRatePct;
        OverdueCount = overdueCount;
        RiskAvg = riskAvg;
    }
}
