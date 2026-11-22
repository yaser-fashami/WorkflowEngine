using Microsoft.EntityFrameworkCore;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Entities;

namespace Workflow.Infrastructure.Persistence.Outbox;

public sealed class WorkflowOutboxClaimRepository : IWorkflowOutboxClaimRepository
{
    private readonly WorkflowDbContext _dbContext;

    public WorkflowOutboxClaimRepository(WorkflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessageClaim>> ClaimBatchAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ValidateArguments(batchSize, nowUtc, leaseExpiresAtUtc, maxAttempts);

        // Pending messages begin a new attempt. An expired Processing lease is reclaimed
        // as the same attempt and therefore does not increment AttemptCount.
        var claimedMessages = await _dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                ;WITH [Candidates] AS
                (
                    SELECT TOP ({batchSize}) *
                    FROM [dbo].[OutboxMessages]
                        WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                    WHERE
                        (
                            [Status] = {(int)OutboxMessageStatus.Pending}
                            AND [NextAttemptAtUtc] <= {nowUtc}
                        )
                        OR
                        (
                            [Status] = {(int)OutboxMessageStatus.Processing}
                            AND [LeaseExpiresAtUtc] <= {nowUtc}
                        )
                    AND [AttemptCount] < {maxAttempts}
                    ORDER BY
                        CASE
                            WHEN [Status] = {(int)OutboxMessageStatus.Pending}
                                THEN [NextAttemptAtUtc]
                            ELSE [LeaseExpiresAtUtc]
                        END,
                        [OccurredAtUtc],
                        [EventId]
                )
                UPDATE [Candidates]
                SET
                    [Status] = {(int)OutboxMessageStatus.Processing},
                    [LeaseToken] = NEWID(),
                    [LeaseExpiresAtUtc] = {leaseExpiresAtUtc},
                    [AttemptCount] = CASE
                        WHEN [Status] = {(int)OutboxMessageStatus.Pending}
                            THEN [AttemptCount] + 1
                        ELSE [AttemptCount]
                    END,
                    [LastAttemptAtUtc] = CASE
                        WHEN [Status] = {(int)OutboxMessageStatus.Pending}
                            THEN {nowUtc}
                        ELSE [LastAttemptAtUtc]
                    END
                OUTPUT inserted.*;
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return claimedMessages
            .Select(message => new OutboxMessageClaim(
                message.EventId,
                message.EventType,
                message.Payload,
                message.OccurredAtUtc,
                message.AttemptCount,
                message.LeaseToken!.Value,
                message.LeaseExpiresAtUtc!.Value))
            .ToArray();
    }

    private static void ValidateArguments(
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc,
        int maxAttempts)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        if (maxAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        if (leaseExpiresAtUtc <= nowUtc)
            throw new ArgumentException(
                "Lease expiration must be later than the claim time.",
                nameof(leaseExpiresAtUtc));

        if (nowUtc.Offset != TimeSpan.Zero || leaseExpiresAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Claim timestamps must be UTC.");
    }
}
