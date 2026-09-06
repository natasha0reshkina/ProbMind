using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class MaterialStudyProgressConfiguration : IEntityTypeConfiguration<MaterialStudyProgress>
{
    public void Configure(EntityTypeBuilder<MaterialStudyProgress> builder)
    {
        builder.ToTable("material_study_progress");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.MaterialStudyCycleId, x.StudentId }).IsUnique();
        builder.HasIndex(x => x.StudentId);
    }
}
