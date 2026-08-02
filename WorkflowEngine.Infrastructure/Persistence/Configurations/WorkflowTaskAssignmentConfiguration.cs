using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTaskAssignmentConfiguration : IEntityTypeConfiguration<WorkflowTaskAssignment>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskAssignment> builder)
    {
        builder.ToTable(
            "WorkflowTaskAssignments");

        builder.HasKey(
            x => x.Id);

        builder.Property(
            x => x.WorkflowTaskId)
            .IsRequired();

        builder.Property(
            x => x.Type)
            .IsRequired();

        builder.Property(
            x => x.Role)
            .HasMaxLength(200);

        builder.HasIndex(
            x => x.WorkflowTaskId);
    }
}
