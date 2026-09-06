using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticTemplateQuestionConfiguration : IEntityTypeConfiguration<DiagnosticTemplateQuestion>
{
    public void Configure(EntityTypeBuilder<DiagnosticTemplateQuestion> builder)
    {
        builder.ToTable("diagnostic_template_questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.DiagnosticTemplateId);
        builder.HasIndex(x => x.QuestionId);
        builder.HasIndex(x => new { x.DiagnosticTemplateId, x.Position }).IsUnique();
        builder.HasIndex(x => new { x.DiagnosticTemplateId, x.QuestionId }).IsUnique();
    }
}
