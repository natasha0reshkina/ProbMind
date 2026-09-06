using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class StudyItemNoteConfiguration : IEntityTypeConfiguration<StudyItemNote>
{
    public void Configure(EntityTypeBuilder<StudyItemNote> builder)
    {
        builder.ToTable("study_item_notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(5000).IsRequired();
        builder.HasIndex(x => x.StudyItemId);
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => new { x.StudyItemId, x.StudentId }).IsUnique();
    }
}
