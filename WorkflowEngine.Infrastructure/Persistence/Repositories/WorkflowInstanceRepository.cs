using Microsoft.EntityFrameworkCore;
using Workflow.Application.Abstractions;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Persistence.Repositories;

public sealed class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly WorkflowDbContext _dbContext;

    public WorkflowInstanceRepository(
        WorkflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkflowInstance?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowInstances
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<WorkflowInstance?>
        GetByIdWithTasksAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowInstances
            .Include(x => x.Tasks)
                .ThenInclude(x => x.Assignments)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            instance);

        await _dbContext.WorkflowInstances
            .AddAsync(
                instance,
                cancellationToken);
    }

    public void Update(
        WorkflowInstance instance)
    {
        ArgumentNullException.ThrowIfNull(
            instance);

        _dbContext.WorkflowInstances
            .Update(instance);
    }
}
