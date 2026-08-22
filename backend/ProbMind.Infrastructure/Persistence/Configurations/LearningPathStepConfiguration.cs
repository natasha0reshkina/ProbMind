using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class LearningPathStepConfiguration : IEntityTypeConfiguration<LearningPathStep>
{
    public void Configure(EntityTypeBuilder<LearningPathStep> builder)
    {
        builder.ToTable("learning_path_steps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(3000);
        builder.HasIndex(x => x.LearningPathId);
        builder.HasIndex(x => x.Position);
        builder.HasIndex(x => x.TopicId);
    }
}
