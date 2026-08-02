using Workflow.Domain.Entities;

public interface IWorkflowTaskAuthorizationService
{
    Task<bool> CanExecuteAsync(
        WorkflowTask task,
        Guid userId,
        CancellationToken cancellationToken);
}
