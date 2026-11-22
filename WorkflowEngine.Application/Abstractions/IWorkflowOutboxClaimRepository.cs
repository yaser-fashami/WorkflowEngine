using Workflow.Application.DTOs;

namespace Workflow.Application.Abstractions;

public interface IWorkflowOutboxClaimRepository
{
    Task<IReadOnlyList<OutboxMessageClaim>> ClaimBatchAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc,
        int maxAttempts,
        CancellationToken cancellationToken);
}
