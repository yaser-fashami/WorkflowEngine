using Workflow.Infrastructure.Persistence;

namespace Workflow.Infrastructure;

public class WorkflowUnitOfWork : IWorkflowUnitOfWork
{
    private readonly WorkflowDbContext _dbContext;

    public WorkflowUnitOfWork(WorkflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
