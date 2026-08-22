using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("activity_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.AggregateType).HasMaxLength(160);
        builder.Property(x => x.PayloadJson).HasMaxLength(30000);
        builder.Property(x => x.CorrelationId).HasMaxLength(80);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAt);
    }
}
