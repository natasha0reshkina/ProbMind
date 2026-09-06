using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticAnswerConfiguration : IEntityTypeConfiguration<DiagnosticAnswer>
{
    public void Configure(EntityTypeBuilder<DiagnosticAnswer> builder)
    {
        builder.ToTable("diagnostic_answers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.QuestionId);
        builder.HasIndex(x => new { x.SessionId, x.QuestionId }).IsUnique();
        builder.Property(x => x.StudentNote).HasMaxLength(2000);
        builder.Property(x => x.Reasoning).HasMaxLength(4000);
        builder.HasIndex(x => x.SubmittedAt);
    }
}
