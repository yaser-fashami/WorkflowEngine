using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowNodeConfiguration : IEntityTypeConfiguration<WorkflowNode>
{
    public void Configure(EntityTypeBuilder<WorkflowNode> builder)
    {
        builder.ToTable(
            "WorkflowNodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.HasMany(
                x => x.OutgoingTransitions)
            .WithOne()
            .HasForeignKey(
                x => x.SourceNodeId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
