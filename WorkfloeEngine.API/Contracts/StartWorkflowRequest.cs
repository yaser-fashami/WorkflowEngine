namespace Workflow.Api.Contracts;
public sealed class StartWorkflowRequest
{
    /// <summary>Stable cross-service workflow code; not a display name or database identifier.</summary>
    public required string WorkflowDefinitionCode { get; init; }
    public required string BusinessType { get; init; }
    public required string BusinessReference { get; init; }
    public required string CorrelationId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required int ContractSchemaVersion { get; init; }
    public object? Input { get; init; }
}
