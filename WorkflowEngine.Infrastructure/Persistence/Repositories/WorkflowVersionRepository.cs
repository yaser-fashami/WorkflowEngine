using Microsoft.EntityFrameworkCore;
using Workflow.Application.Abstractions;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Persistence;

namespace Workflow.Infrastructure.Persistence.Repositories;

public sealed class WorkflowVersionRepository : IWorkflowVersionRepository
{
    private readonly WorkflowDbContext _dbContext;

    public WorkflowVersionRepository(
        WorkflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkflowVersion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WorkflowVersions
            .AsNoTracking()
            .Include(x => x.Nodes)
                .ThenInclude(x => x.OutgoingTransitions)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

public async Task<WorkflowVersion?> GetPublishedAsync(
        Guid workflowDefinitionId,
        CancellationToken cancellationToken)
{
    return await _dbContext.WorkflowVersions
        .AsNoTracking()
        .Include(x => x.Nodes)
            .ThenInclude(x =>
                x.OutgoingTransitions)
        .SingleOrDefaultAsync(
            x =>
                x.WorkflowDefinitionId ==
                    workflowDefinitionId
                &&
                x.Status ==
                    WorkflowVersionStatus.Published,
            cancellationToken);
}
    public async Task AddAsync(
        WorkflowVersion version,
        CancellationToken cancellationToken)
    {
        await _dbContext.WorkflowVersions
            .AddAsync(
                version,
                cancellationToken);
    }
}
