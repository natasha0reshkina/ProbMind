using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class MisconceptionConfiguration : IEntityTypeConfiguration<Misconception>
{
    public void Configure(EntityTypeBuilder<Misconception> builder)
    {
        builder.ToTable("misconceptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(120);
        builder.Property(x => x.Title).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(8000);
        builder.Property(x => x.CorrectiveExplanation).HasMaxLength(12000);
        builder.Property(x => x.DiagnosticRationale).HasMaxLength(6000);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.Status);
    }
}
