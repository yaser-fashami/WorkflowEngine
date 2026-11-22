namespace Workflow.Infrastructure.Persistence.Entities;

public enum OutboxMessageStatus { Pending = 0, Processing = 1, Published = 2, DeadLetter = 3 }

public sealed class OutboxMessage
{
    private OutboxMessage() { }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage Create(Guid eventId, string eventType, string payload, DateTimeOffset occurredAtUtc)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id is required.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(eventType) || eventType.Length > 200)
            throw new ArgumentException("A valid event type is required.", nameof(eventType));
        ArgumentNullException.ThrowIfNull(payload);
        return new OutboxMessage
        {
            EventId = eventId, EventType = eventType, Payload = payload,
            OccurredAtUtc = occurredAtUtc, NextAttemptAtUtc = occurredAtUtc,
            Status = OutboxMessageStatus.Pending, AttemptCount = 0
        };
    }
}
