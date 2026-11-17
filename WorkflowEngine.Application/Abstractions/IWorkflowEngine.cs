using Workflow.Application.Commands;
using Workflow.Application.DTOs;
using Workflow.Domain.Entities;

namespace Workflow.Application.Abstractions;

public interface IWorkflowEngine
{
    Task<WorkflowInstanceDto> StartAsync(
        StartWorkflowCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthorizationCheckDto> CheckAuthorizationAsync(\n        Guid workflowInstanceId,\n        Guid taskId,\n        string action,\n        Guid actorUserId,\n        CancellationToken cancellationToken = default);\n\n    Task<WorkflowInstanceDto> ExecuteActionAsync(
        ExecuteWorkflowActionCommand command,
        CancellationToken cancellationToken = default);
}
