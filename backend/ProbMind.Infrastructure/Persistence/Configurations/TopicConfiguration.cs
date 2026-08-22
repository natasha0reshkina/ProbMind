using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.NameRu).HasMaxLength(200);
        builder.Property(x => x.NameEn).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.SortOrder);
    }
}
