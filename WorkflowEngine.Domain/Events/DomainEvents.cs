namespace Workflow.Domain.Events;

using Workflow.Domain.Common;

public sealed record WorkflowCompletedDomainEvent(
    Guid WorkflowInstanceId,
    string EntityType,
    Guid EntityId)
    : IDomainEvent
{
    public DateTimeOffset OccurredOn
        => DateTimeOffset.UtcNow;
}

public sealed record WorkflowTaskCreatedDomainEvent(
    Guid WorkflowInstanceId,
    Guid TaskId,
    Guid NodeId)
    : IDomainEvent
{
    public DateTimeOffset OccurredOn
        => DateTimeOffset.UtcNow;
}
