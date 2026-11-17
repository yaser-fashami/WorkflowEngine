namespace Workflow.Api.Contracts;

/// <summary>
/// Request to check whether a human actor may execute an action on a workflow task.
/// </summary>
public sealed class CheckWorkflowAuthorizationRequest
{
    /// <summary>Identifier of the task.</summary>
    public required Guid TaskId { get; init; }

    /// <summary>Business action to check against the workflow transitions.</summary>
    public required string Action { get; init; }

    /// <summary>Stable identifier of the human actor authenticated by SinaHRM.</summary>
    public required Guid ActorUserId { get; init; }
}
