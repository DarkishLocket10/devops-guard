using DevOpsGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DevOpsGuard.Infrastructure.Data.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("WorkItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Service)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Priority)
            .HasConversion<int>()
            .IsRequired();

        // DateOnly? -> SQL Server 'date' (nullable)
        var dateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null
        );

        builder.Property(x => x.DueDate)
            .HasConversion(dateOnlyConverter)
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Component)
            .HasMaxLength(100);

        builder.Property(x => x.Assignee)
            .HasMaxLength(100);

        // List<string> Labels stored as a comma-separated string (null-safe both ways)
        var labelsConverter = new ValueConverter<List<string>, string>(
            v => v == null ? string.Empty : string.Join(",", v),
            v => string.IsNullOrWhiteSpace(v)
                    ? new List<string>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .ToList()
        );

        // Expression-tree-friendly comparer (all lambdas are expression-bodied)
        var labelsComparer = new ValueComparer<List<string>>(
            (a, b) =>
                ReferenceEquals(a, b) ||
                ((a == null || b == null) ? a == b : a.SequenceEqual(b)),
            v => v == null
                ? 0
                : v.Aggregate(0, (h, s) => unchecked(h * 31 + (s == null ? 0 : s.GetHashCode()))),
            v => v == null ? new List<string>() : v.ToList()
        );

        builder.Property(x => x.Labels)
            .HasConversion(labelsConverter)
            .Metadata.SetValueComparer(labelsComparer);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.Service);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UpdatedAtUtc);
        builder.HasIndex(x => x.DueDate);
    }
}
