namespace Workflow.Application.Commands;

public sealed record ExecuteWorkflowActionCommand(
    Guid WorkflowInstanceId,
    Guid TaskId,
    string Action,
    Guid PerformedBy);
