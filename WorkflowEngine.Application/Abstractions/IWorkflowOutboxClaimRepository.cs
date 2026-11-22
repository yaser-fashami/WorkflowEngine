using Workflow.Application.DTOs;
namespace Workflow.Application.Abstractions;
public interface IWorkflowOutboxClaimRepository
{
    Task<IReadOnlyList<OutboxMessageClaim>> ClaimBatchAsync(int batchSize, DateTimeOffset nowUtc, DateTimeOffset leaseExpiresAtUtc, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> MarkPublishedAsync(Guid eventId, Guid leaseToken, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken);
    Task<bool> ScheduleRetryAsync(Guid eventId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc, string error, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> MoveToDeadLetterAsync(Guid eventId, Guid leaseToken, string error, CancellationToken cancellationToken);
}