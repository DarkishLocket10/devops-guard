namespace DevOpsGuard.Application.DTOs;

public sealed record MetricsHistoryPoint(
    DateTime CapturedAtUtc,
    double BacklogHealthPct,
    double SlaBreachRatePct,
    int OverdueCount,
    double RiskAvg
);

public sealed record MetricsHistoryResponse(
    int Count,
    List<MetricsHistoryPoint> Points
);
