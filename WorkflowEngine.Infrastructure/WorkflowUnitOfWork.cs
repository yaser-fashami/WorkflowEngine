using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Workflow.Application.Abstractions;
using Workflow.Infrastructure.Persistence;
using Workflow.Application.Services;

namespace Workflow.Infrastructure;

public class WorkflowUnitOfWork : IWorkflowUnitOfWork
{
    private const string IndexName = "UX_WorkflowInstances_BusinessScope_IdempotencyKey";
    private readonly WorkflowDbContext _dbContext;
    public WorkflowUnitOfWork(WorkflowDbContext dbContext) => _dbContext = dbContext;
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await _dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (IsIdempotencyViolation(exception))
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).ToArray()) entry.State = EntityState.Detached;
            throw new WorkflowEngine.WorkflowInstanceIdempotencyRaceException(exception);
        }
    }
    private static bool IsIdempotencyViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
            if (current is SqlException sql && (sql.Number == 2601 || sql.Number == 2627) && sql.Message.Contains(IndexName, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
