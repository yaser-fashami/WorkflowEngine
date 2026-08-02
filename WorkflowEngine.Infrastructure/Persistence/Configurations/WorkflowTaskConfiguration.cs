using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable(
            "WorkflowTasks");

        builder.HasKey(
            x => x.Id);

        builder.Property(
            x => x.WorkflowInstanceId)
            .IsRequired();

        builder.Property(
            x => x.WorkflowNodeId)
            .IsRequired();

        builder.Property(
            x => x.TaskType)
            .IsRequired();

        builder.Property(
            x => x.Status)
            .IsRequired();

        builder.Property(
            x => x.CreatedAt)
            .IsRequired();

        builder.HasMany(
            x => x.Assignments)
            .WithOne()
            .HasForeignKey(
                x => x.WorkflowTaskId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}