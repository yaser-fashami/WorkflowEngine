using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable(
            "WorkflowDefinitions");

        builder.HasKey(
            x => x.Id);

        builder.Property(
                x => x.Id)
            .ValueGeneratedNever();

        builder.Property(
                x => x.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(
                x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(
                x => x.Description)
            .HasMaxLength(1000);

        builder.Property(
                x => x.IsActive)
            .IsRequired();

        builder.Property(
                x => x.CreatedAt)
            .IsRequired();

        builder.Property(
                x => x.UpdatedAt);

        builder.HasIndex(
                x => x.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_WorkflowDefinitions_Code");

        builder.HasMany(
                x => x.Versions)
            .WithOne()
            .HasForeignKey(
                x => x.WorkflowDefinitionId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
