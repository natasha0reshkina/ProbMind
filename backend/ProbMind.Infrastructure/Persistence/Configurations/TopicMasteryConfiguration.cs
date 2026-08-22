using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class TopicMasteryConfiguration : IEntityTypeConfiguration<TopicMastery>
{
    public void Configure(EntityTypeBuilder<TopicMastery> builder)
    {
        builder.ToTable("topic_masterys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.Mastery);
        builder.HasIndex(x => new { x.UserId, x.TopicId }).IsUnique();
    }
}
