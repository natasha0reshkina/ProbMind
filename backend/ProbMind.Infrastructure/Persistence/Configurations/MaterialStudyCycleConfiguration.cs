using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class MaterialStudyCycleConfiguration : IEntityTypeConfiguration<MaterialStudyCycle>
{
    public void Configure(EntityTypeBuilder<MaterialStudyCycle> builder)
    {
        builder.ToTable("material_study_cycles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MaterialText).HasMaxLength(20000).IsRequired();
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.IsPublished);
    }
}
