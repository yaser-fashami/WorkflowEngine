namespace Workflow.Api.Contracts;

/// <summary>
/// HTTP contract for starting a workflow.
/// </summary>
public sealed class StartWorkflowRequest
{
    /// <summary>
    /// Stable, unique code agreed between WorkflowEngine and its calling services.
    /// This is the integration identifier: it is not a display name and must not
    /// expose or depend on WorkflowDefinitionId from the WorkflowEngine database.
    /// WorkflowEngine resolves this code to its internal definition identifier.
    /// </summary>
    public required string WorkflowDefinitionCode { get; init; }

    public object? Input { get; init; }
}
