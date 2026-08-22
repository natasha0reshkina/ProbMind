using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class QuestionMisconceptionMapConfiguration : IEntityTypeConfiguration<QuestionMisconceptionMap>
{
    public void Configure(EntityTypeBuilder<QuestionMisconceptionMap> builder)
    {
        builder.ToTable("question_misconception_maps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.QuestionId);
        builder.HasIndex(x => x.MisconceptionId);
        builder.HasIndex(x => new { x.QuestionId, x.MisconceptionId }).IsUnique();
    }
}
