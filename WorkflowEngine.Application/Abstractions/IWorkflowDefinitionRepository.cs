using Workflow.Domain.Entities;

namespace Workflow.Application.Abstractions;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<WorkflowDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<WorkflowDefinition?> GetByCodeWithVersionsAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);

    void Update(
        WorkflowDefinition definition);

    void Remove(
        WorkflowDefinition definition);
}
