using DevOpsGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevOpsGuard.Infrastructure.Data.Configurations;

public sealed class MetricsSnapshotConfiguration : IEntityTypeConfiguration<MetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<MetricsSnapshot> builder)
    {
        builder.ToTable("MetricsSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CapturedAtUtc).IsRequired();
        builder.Property(x => x.BacklogHealthPct).IsRequired();
        builder.Property(x => x.SlaBreachRatePct).IsRequired();
        builder.Property(x => x.OverdueCount).IsRequired();
        builder.Property(x => x.RiskAvg).IsRequired();

        builder.HasIndex(x => x.CapturedAtUtc);
    }
}
