using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class QuestionExposureConfiguration : IEntityTypeConfiguration<QuestionExposure>
{
    public void Configure(EntityTypeBuilder<QuestionExposure> builder)
    {
        builder.ToTable("question_exposures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.QuestionId);
        builder.HasIndex(x => x.NextEligibleAt);
        builder.HasIndex(x => new { x.UserId, x.QuestionId }).IsUnique();
    }
}
