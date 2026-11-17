using Workflow.Domain.Entities;

namespace Workflow.Application.Abstractions;

public interface IWorkflowTaskAuthorizationService
{
    Task<bool> CanExecuteAsync(
        WorkflowTask task,
        Guid userId,
        CancellationToken cancellationToken);
}
