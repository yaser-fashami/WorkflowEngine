using Workflow.Domain.Entities;

namespace Workflow.Application.Abstractions;

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetByIdWithTasksAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default);

    void Update(
        WorkflowInstance instance);
}