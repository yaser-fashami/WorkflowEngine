namespace Workflow.Application.Abstractions;

using Workflow.Domain.Entities;

public interface IWorkflowVersionRepository
{
    Task<WorkflowVersion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<WorkflowVersion?> GetPublishedAsync(
        Guid workflowDefinitionId,
        CancellationToken cancellationToken);

    Task AddAsync(
        WorkflowVersion version,
        CancellationToken cancellationToken);
}