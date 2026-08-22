using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class PracticeAttemptConfiguration : IEntityTypeConfiguration<PracticeAttempt>
{
    public void Configure(EntityTypeBuilder<PracticeAttempt> builder)
    {
        builder.ToTable("practice_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.PracticeSessionId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.QuestionId);
    }
}
