using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticTemplateConfiguration : IEntityTypeConfiguration<DiagnosticTemplate>
{
    public void Configure(EntityTypeBuilder<DiagnosticTemplate> builder)
    {
        builder.ToTable("diagnostic_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.IsPublished);
        builder.HasIndex(x => x.CreatedAt);
    }
}
