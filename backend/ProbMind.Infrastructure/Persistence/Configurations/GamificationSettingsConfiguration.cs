using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class GamificationSettingsConfiguration : IEntityTypeConfiguration<GamificationSettings>
{
    public void Configure(EntityTypeBuilder<GamificationSettings> builder)
    {
        builder.ToTable("gamification_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.LeaderboardEnabled).IsRequired();
    }
}
