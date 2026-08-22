using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class QuestionVersionConfiguration : IEntityTypeConfiguration<QuestionVersion>
{
    public void Configure(EntityTypeBuilder<QuestionVersion> builder)
    {
        builder.ToTable("question_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Prompt).HasMaxLength(8000);
        builder.Property(x => x.CorrectExplanation).HasMaxLength(12000);
        builder.Property(x => x.AuthorNotes).HasMaxLength(4000);
        builder.HasIndex(x => x.QuestionId);
        builder.HasIndex(x => x.VersionNumber);
        builder.HasIndex(x => new { x.QuestionId, x.VersionNumber }).IsUnique();
    }
}
