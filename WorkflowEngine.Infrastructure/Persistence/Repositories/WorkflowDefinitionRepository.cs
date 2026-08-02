using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Application.Abstractions;

namespace Workflow.Infrastructure.Persistence.Repositories;

public sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly WorkflowDbContext _context;

    public WorkflowDefinitionRepository(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(
                x => x.Code == code,
                cancellationToken);
    }

    public async Task<WorkflowDefinition?>
        GetByCodeWithVersionsAsync(
            string code,
            CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(
                x => x.Code == code,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await _context.WorkflowDefinitions
            .AddAsync(
                definition,
                cancellationToken);
    }

    public void Update(
        WorkflowDefinition definition)
    {
        _context.WorkflowDefinitions
            .Update(definition);
    }

    public void Remove(
        WorkflowDefinition definition)
    {
        _context.WorkflowDefinitions
            .Remove(definition);
    }
}
