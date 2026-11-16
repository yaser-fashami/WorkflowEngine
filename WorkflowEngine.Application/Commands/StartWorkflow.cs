namespace Workflow.Application.Commands;

/// <summary>
/// Represents a request to start a workflow using its stable public code.
/// </summary>
public sealed record StartWorkflowCommand
{
    /// <summary>
    /// Gets the stable, unique workflow code shared with calling services.
    /// This is not the display name and must not be the database-specific WorkflowDefinitionId.
    /// WorkflowEngine resolves this code to its internal definition identifier.
    /// </summary>
    public required string WorkflowDefinitionCode { get; init; }

    /// <summary>
    /// Gets the authenticated user who initiated the workflow.
    /// </summary>
    public required Guid StartedBy { get; init; }
    public required string BusinessType { get; init; }
    public required string BusinessReference { get; init; }
    public required string CorrelationId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required int ContractSchemaVersion { get; init; }

    /// <summary>
    /// Gets the input payload used to initialize the workflow context.
    /// </summary>
    public object? Input { get; init; }
}
