using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class SpacedReviewItemConfiguration : IEntityTypeConfiguration<SpacedReviewItem>
{
    public void Configure(EntityTypeBuilder<SpacedReviewItem> builder)
    {
        builder.ToTable("spaced_review_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.TopicId }).IsUnique();
        builder.HasIndex(x => x.NextReviewAt);
    }
}
