using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Infrastructure.Persistence.Entities;

namespace Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
    }

    public DbSet<WorkflowDefinition>
        WorkflowDefinitions
        => Set<WorkflowDefinition>();

    public DbSet<WorkflowVersion>
        WorkflowVersions
        => Set<WorkflowVersion>();

    public DbSet<WorkflowNode>
        WorkflowNodes
        => Set<WorkflowNode>();

    public DbSet<WorkflowTransition>
        WorkflowTransitions
        => Set<WorkflowTransition>();

    public DbSet<WorkflowInstance>
        WorkflowInstances
        => Set<WorkflowInstance>();

    public DbSet<WorkflowTask>
        WorkflowTasks
        => Set<WorkflowTask>();

    public DbSet<WorkflowTaskAssignment> WorkflowTaskAssignments => Set<WorkflowTaskAssignment>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }
}
