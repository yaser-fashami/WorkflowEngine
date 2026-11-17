using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable(
            "WorkflowInstances");

        builder.HasKey(
            x => x.Id);

        builder.Property(
                x => x.Id)
            .ValueGeneratedNever();

        builder.Property(
                x => x.WorkflowDefinitionId)
            .IsRequired();

        builder.Property(x => x.WorkflowVersionId).IsRequired();
        builder.Property(x => x.BusinessType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BusinessReference).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContractSchemaVersion).IsRequired();
        builder.Property(x => x.InputJson).IsRequired();
        builder.Property(x => x.RequestFingerprint).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.DocumentId).HasMaxLength(200);
        builder.Property(x => x.DocumentVersion).HasMaxLength(100);
        builder.Property(x => x.DocumentHash).HasMaxLength(256);
        builder.HasIndex(x => new { x.BusinessType, x.BusinessReference, x.IdempotencyKey })
            .IsUnique().HasDatabaseName("UX_WorkflowInstances_BusinessScope_IdempotencyKey");

        builder.Property(
                x => x.CurrentNodeId);

        builder.Property(
                x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(
                x => x.StartedBy);

        builder.Property(x => x.StartedByService)
            .HasMaxLength(100);

        builder.Property(
                x => x.StartedAt);

        builder.Property(
                x => x.CompletedBy);

        builder.Property(
                x => x.CompletedAt);

        builder.HasIndex(
                x => x.WorkflowDefinitionId)
            .HasDatabaseName(
                "IX_WorkflowInstances_WorkflowDefinitionId");

        builder.HasIndex(
                x => x.WorkflowVersionId)
            .HasDatabaseName(
                "IX_WorkflowInstances_WorkflowVersionId");

        builder.HasIndex(
                x => x.CurrentNodeId)
            .HasDatabaseName(
                "IX_WorkflowInstances_CurrentNodeId");

        builder.HasIndex(
                x => x.Status)
            .HasDatabaseName(
                "IX_WorkflowInstances_Status");

        builder.HasOne<WorkflowDefinition>()
            .WithMany()
            .HasForeignKey(
                x => x.WorkflowDefinitionId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<WorkflowVersion>()
            .WithMany()
            .HasForeignKey(
                x => x.WorkflowVersionId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<WorkflowNode>()
            .WithMany()
            .HasForeignKey(
                x => x.CurrentNodeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasMany(
                x => x.Tasks)
            .WithOne()
            .HasForeignKey(
                x => x.WorkflowInstanceId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
