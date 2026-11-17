namespace Workflow.Api.Contracts;

/// <summary>
/// Request to execute an action on a pending task in a workflow instance.
/// </summary>
public sealed class ExecuteWorkflowActionRequest
{
    /// <summary>Identifier of the task on which the action is executed.</summary>
    public required Guid TaskId { get; init; }

    /// <summary>Business action to execute, as defined by the workflow transition.</summary>
    public required string Action { get; init; }
}
