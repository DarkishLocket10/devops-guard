namespace DevOpsGuard.Application.DTOs;

public sealed record MetricsResponse(
    double BacklogHealthPct,
    double SlaBreachRatePct,
    int OverdueCount,
    RiskSummary Risk
);

public sealed record RiskSummary(double Avg);
