using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class StudyItemConfiguration : IEntityTypeConfiguration<StudyItem>
{
    public void Configure(EntityTypeBuilder<StudyItem> builder)
    {
        builder.ToTable("study_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(10000).IsRequired();
        builder.Property(x => x.TeacherResponse).HasMaxLength(5000).IsRequired();
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.Visibility);
        builder.HasIndex(x => x.CreatedAt);
    }
}
