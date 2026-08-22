using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class TopicMasteryHistoryConfiguration : IEntityTypeConfiguration<TopicMasteryHistory>
{
    public void Configure(EntityTypeBuilder<TopicMasteryHistory> builder)
    {
        builder.ToTable("topic_mastery_historys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TopicId);
        builder.HasIndex(x => x.RecordedAt);
    }
}
