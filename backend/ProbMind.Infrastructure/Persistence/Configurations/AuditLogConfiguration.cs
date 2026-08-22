using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(160);
        builder.Property(x => x.OldValueJson).HasMaxLength(30000);
        builder.Property(x => x.NewValueJson).HasMaxLength(30000);
        builder.Property(x => x.RequestId).HasMaxLength(100);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.CreatedAt);
    }
}
