namespace Workflow.Api.Contracts;

/// <summary>
/// Request to start a workflow for a business object.
/// </summary>
public sealed class StartWorkflowRequest
{
    /// <summary>
    /// Stable cross-service workflow code; not a display name or database identifier.
    /// </summary>
    public required string WorkflowDefinitionCode { get; init; }

    /// <summary>Business type that identifies the owning business context.</summary>
    public required string BusinessType { get; init; }

    /// <summary>Business object reference within the specified business type.</summary>
    public required string BusinessReference { get; init; }

    /// <summary>Tracing identifier. It is not used as an idempotency key.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Idempotency key scoped to BusinessType and BusinessReference.</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Version of the input contract supplied by the calling service.</summary>
    public required int ContractSchemaVersion { get; init; }

    /// <summary>Optional opaque identifier of the associated document.</summary>
    public string? DocumentId { get; init; }
    /// <summary>Optional version of the associated document.</summary>
    public string? DocumentVersion { get; init; }
    /// <summary>Optional hash of the exact document version.</summary>
    public string? DocumentHash { get; init; }

    /// <summary>Workflow-specific input data.</summary>
    public object? Input { get; init; }
}
