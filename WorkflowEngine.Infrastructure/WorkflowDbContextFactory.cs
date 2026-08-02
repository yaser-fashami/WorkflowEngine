using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Workflow.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.Persistence;

public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<WorkflowDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;" +
            "Initial Catalog=WorkflowEngineDb;" +
            "User Id=sa;Password=Tomcat745" +
            "Trusted_Connection=True;" +
            "Encrypt=false;" +
            "TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(
                    typeof(WorkflowDbContext)
                        .Assembly
                        .GetName()
                        .Name);
            });

        return new WorkflowDbContext(
            optionsBuilder.Options);
    }
}