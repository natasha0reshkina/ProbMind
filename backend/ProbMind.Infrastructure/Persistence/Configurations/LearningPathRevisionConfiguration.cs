using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class LearningPathRevisionConfiguration : IEntityTypeConfiguration<LearningPathRevision>
{
    public void Configure(EntityTypeBuilder<LearningPathRevision> builder)
    {
        builder.ToTable("learning_path_revisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.SnapshotJson).HasMaxLength(30000);
        builder.Property(x => x.ChangeReason).HasMaxLength(1000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.RevisionNumber);
    }
}
