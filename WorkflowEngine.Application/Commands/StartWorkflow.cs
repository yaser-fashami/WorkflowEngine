namespace Workflow.Application.Commands;


public sealed record StartWorkflowCommand(
    Guid WorkflowDefinitionId,
    Guid StartedBy,
    object? Input = null);
