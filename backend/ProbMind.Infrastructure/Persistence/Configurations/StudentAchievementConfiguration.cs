using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class StudentAchievementConfiguration : IEntityTypeConfiguration<StudentAchievement>
{
    public void Configure(EntityTypeBuilder<StudentAchievement> builder)
    {
        builder.ToTable("student_achievements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Code).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Code }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UnlockedAt);
    }
}
