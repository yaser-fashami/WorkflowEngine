using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.ToTable(
            "WorkflowVersions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x =>
            x.WorkflowDefinitionId)
            .IsRequired();

        builder.Property(x =>
            x.VersionNumber)
            .IsRequired();

        builder.Property(x =>
            x.Status)
            .IsRequired();

        builder.HasMany(x => x.Nodes)
            .WithOne()
            .HasForeignKey(x =>
                x.WorkflowVersionId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasIndex(
            x => new
            {
                x.WorkflowDefinitionId,
                x.VersionNumber
            })
            .IsUnique();
    }
}
