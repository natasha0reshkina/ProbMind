using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticReportConfiguration : IEntityTypeConfiguration<DiagnosticReport>
{
    public void Configure(EntityTypeBuilder<DiagnosticReport> builder)
    {
        builder.ToTable("diagnostic_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.SummaryMarkdown).HasMaxLength(12000);
        builder.Property(x => x.StrongestTopicCode).HasMaxLength(120);
        builder.Property(x => x.WeakestTopicCode).HasMaxLength(120);
        builder.HasIndex(x => x.SessionId).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.GeneratedAt);
    }
}
