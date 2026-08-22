using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class CorrectiveModuleConfiguration : IEntityTypeConfiguration<CorrectiveModule>
{
    public void Configure(EntityTypeBuilder<CorrectiveModule> builder)
    {
        builder.ToTable("corrective_modules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.TheoryMarkdown).HasMaxLength(20000);
        builder.Property(x => x.WorkedExampleMarkdown).HasMaxLength(20000);
        builder.HasIndex(x => x.MisconceptionId);
        builder.HasIndex(x => x.Status);
    }
}
