using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticSessionConfiguration : IEntityTypeConfiguration<DiagnosticSession>
{
    public void Configure(EntityTypeBuilder<DiagnosticSession> builder)
    {
        builder.ToTable("diagnostic_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.SelectionPolicyVersion).HasMaxLength(80);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
