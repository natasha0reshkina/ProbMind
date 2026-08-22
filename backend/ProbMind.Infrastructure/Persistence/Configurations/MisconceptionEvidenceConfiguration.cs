using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class MisconceptionEvidenceConfiguration : IEntityTypeConfiguration<MisconceptionEvidence>
{
    public void Configure(EntityTypeBuilder<MisconceptionEvidence> builder)
    {
        builder.ToTable("misconception_evidences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Explanation).HasMaxLength(5000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.MisconceptionId);
        builder.HasIndex(x => x.ObservedAt);
        builder.HasIndex(x => x.Kind);
    }
}
