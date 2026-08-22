using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("recommendations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.Rationale).HasMaxLength(4000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.IsDismissed);
    }
}
