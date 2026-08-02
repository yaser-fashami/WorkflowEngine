using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;

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

    public DbSet<WorkflowTaskAssignment>
        WorkflowTaskAssignments =>
        Set<WorkflowTaskAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }
}
