using Workflow.Application.DTOs;

namespace Workflow.Application.Abstractions;

public interface IWorkflowOutbox
{
    Task AddAsync(WorkflowCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
