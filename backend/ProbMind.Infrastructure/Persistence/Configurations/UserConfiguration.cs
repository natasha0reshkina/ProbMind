using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.PasswordHash).HasMaxLength(800);
        builder.Property(x => x.DisplayName).HasMaxLength(120);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Role);
        builder.HasIndex(x => x.IsActive);
    }
}
