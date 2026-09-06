using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence.Configurations;

public sealed class TeacherInterventionConfiguration : IEntityTypeConfiguration<TeacherIntervention>
{
    public void Configure(EntityTypeBuilder<TeacherIntervention> builder)
    {
        builder.ToTable("teacher_interventions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.IsCompleted);
    }
}
