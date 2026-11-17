namespace Workflow.Application.DTOs;

public sealed class WorkflowCompletedIntegrationEvent
{
    public required Guid EventId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public required string BusinessType { get; init; }
    public required string BusinessReference { get; init; }
    public required DateTimeOffset CompletedAtUtc { get; init; }
    public string? DocumentId { get; init; }
    public string? DocumentVersion { get; init; }
    public string? DocumentHash { get; init; }
}
