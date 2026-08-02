using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable(
            "WorkflowTransitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasMaxLength(100);

        builder.Property(
            x => x.ConditionExpression)
            .HasMaxLength(2000);

        builder.HasOne<WorkflowNode>()
            .WithMany()
            .HasForeignKey(
                x => x.TargetNodeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
