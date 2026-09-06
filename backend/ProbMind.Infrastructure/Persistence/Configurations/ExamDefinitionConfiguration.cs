using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class ExamDefinitionConfiguration : IEntityTypeConfiguration<ExamDefinition>
{
    public void Configure(EntityTypeBuilder<ExamDefinition> builder)
    {
        builder.ToTable("exam_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.DiagnosticTemplateId);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.IsPublished);
    }
}
