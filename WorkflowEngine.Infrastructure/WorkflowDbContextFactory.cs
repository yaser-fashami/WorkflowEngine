using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Workflow.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.Persistence;

public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__WorkflowDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The design-time connection string is not configured. " +
                "Set the ConnectionStrings__WorkflowDatabase environment variable.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<WorkflowDbContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(
                    typeof(WorkflowDbContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        return new WorkflowDbContext(optionsBuilder.Options);
    }
}
