namespace Workflow.Application.DTOs;

public sealed record OutboxMessageClaim(
    Guid EventId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAtUtc,
    int AttemptCount,
    Guid LeaseToken,
    DateTimeOffset LeaseExpiresAtUtc);
