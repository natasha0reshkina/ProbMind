using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class UserMisconceptionConfiguration : IEntityTypeConfiguration<UserMisconception>
{
    public void Configure(EntityTypeBuilder<UserMisconception> builder)
    {
        builder.ToTable("user_misconceptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.MisconceptionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Confidence);
        builder.HasIndex(x => new { x.UserId, x.MisconceptionId }).IsUnique();
    }
}
