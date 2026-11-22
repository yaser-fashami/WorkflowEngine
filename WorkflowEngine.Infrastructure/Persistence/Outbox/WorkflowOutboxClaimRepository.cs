using Microsoft.EntityFrameworkCore;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Entities;
namespace Workflow.Infrastructure.Persistence.Outbox;
public sealed class WorkflowOutboxClaimRepository : IWorkflowOutboxClaimRepository
{
    private readonly WorkflowDbContext _dbContext;
    public WorkflowOutboxClaimRepository(WorkflowDbContext dbContext) => _dbContext = dbContext;
    public async Task<IReadOnlyList<OutboxMessageClaim>> ClaimBatchAsync(int batchSize, DateTimeOffset nowUtc, DateTimeOffset leaseExpiresAtUtc, int maxAttempts, CancellationToken cancellationToken)
    {
        ValidateClaimArguments(batchSize, nowUtc, leaseExpiresAtUtc, maxAttempts);
        var claimed = await _dbContext.OutboxMessages.FromSqlInterpolated($"""
            ;WITH [Candidates] AS
            (
                SELECT TOP ({batchSize}) *
                FROM [dbo].[OutboxMessages] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                WHERE (([Status] = {(int)OutboxMessageStatus.Pending} AND [NextAttemptAtUtc] <= {nowUtc})
                    OR ([Status] = {(int)OutboxMessageStatus.Processing} AND [LeaseExpiresAtUtc] <= {nowUtc}))
                    AND [AttemptCount] < {maxAttempts}
                ORDER BY CASE WHEN [Status] = {(int)OutboxMessageStatus.Pending} THEN [NextAttemptAtUtc] ELSE [LeaseExpiresAtUtc] END, [OccurredAtUtc], [EventId]
            )
            UPDATE [Candidates]
            SET [Status] = {(int)OutboxMessageStatus.Processing}, [LeaseToken] = NEWID(), [LeaseExpiresAtUtc] = {leaseExpiresAtUtc},
                [AttemptCount] = CASE WHEN [Status] = {(int)OutboxMessageStatus.Pending} THEN [AttemptCount] + 1 ELSE [AttemptCount] END,
                [TotalAttemptCount] = CASE WHEN [Status] = {(int)OutboxMessageStatus.Pending} THEN [TotalAttemptCount] + 1 ELSE [TotalAttemptCount] END,
                [LastAttemptAtUtc] = CASE WHEN [Status] = {(int)OutboxMessageStatus.Pending} THEN {nowUtc} ELSE [LastAttemptAtUtc] END
            OUTPUT inserted.*;
            """).AsNoTracking().ToListAsync(cancellationToken);
        return claimed.Select(x => new OutboxMessageClaim(x.EventId, x.EventType, x.Payload, x.OccurredAtUtc, x.AttemptCount, x.LeaseToken!.Value, x.LeaseExpiresAtUtc!.Value)).ToArray();
    }
    public async Task<bool> MarkPublishedAsync(Guid eventId, Guid leaseToken, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken)
    {
        ValidateLease(eventId, leaseToken, publishedAtUtc);
        var affected = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [dbo].[OutboxMessages]
            SET [Status] = {(int)OutboxMessageStatus.Published}, [PublishedAtUtc] = {publishedAtUtc}, [LeaseToken] = NULL, [LeaseExpiresAtUtc] = NULL, [LastError] = NULL
            WHERE [EventId] = {eventId} AND [LeaseToken] = {leaseToken} AND [Status] = {(int)OutboxMessageStatus.Processing}
              AND [LeaseExpiresAtUtc] > {publishedAtUtc};
            """, cancellationToken);
        return affected == 1;
    }
    public async Task<bool> ScheduleRetryAsync(Guid eventId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc, string error, int maxAttempts, CancellationToken cancellationToken)
    {
        ValidateLease(eventId, leaseToken, nextAttemptAtUtc);
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        var safeError = NormalizeError(error);
        var affected = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [dbo].[OutboxMessages]
            SET [Status] = CASE WHEN [AttemptCount] >= {maxAttempts} THEN {(int)OutboxMessageStatus.DeadLetter} ELSE {(int)OutboxMessageStatus.Pending} END,
                [NextAttemptAtUtc] = {nextAttemptAtUtc}, [LastError] = {safeError},
                [DeadLetterAtUtc] = CASE WHEN [AttemptCount] >= {maxAttempts} THEN SYSUTCDATETIME() ELSE [DeadLetterAtUtc] END,
                [LeaseToken] = NULL, [LeaseExpiresAtUtc] = NULL
            WHERE [EventId] = {eventId} AND [LeaseToken] = {leaseToken} AND [Status] = {(int)OutboxMessageStatus.Processing}
              AND [LeaseExpiresAtUtc] > SYSUTCDATETIME();
            """, cancellationToken);
        return affected == 1;
    }
    public async Task<bool> MoveToDeadLetterAsync(Guid eventId, Guid leaseToken, string error, CancellationToken cancellationToken)
    {
        ValidateIds(eventId, leaseToken);
        var safeError = NormalizeError(error);
        var affected = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [dbo].[OutboxMessages]
            SET [Status] = {(int)OutboxMessageStatus.DeadLetter}, [LastError] = {safeError}, [DeadLetterAtUtc] = SYSUTCDATETIME(),
                [LeaseToken] = NULL, [LeaseExpiresAtUtc] = NULL
            WHERE [EventId] = {eventId} AND [LeaseToken] = {leaseToken} AND [Status] = {(int)OutboxMessageStatus.Processing}
              AND [LeaseExpiresAtUtc] > SYSUTCDATETIME();
            """, cancellationToken);
        return affected == 1;
    }
    public async Task<bool> ReplayDeadLetterAsync(Guid eventId, DateTimeOffset replayedAtUtc, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        EnsureUtc(replayedAtUtc, nameof(replayedAtUtc));
        var affected = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [dbo].[OutboxMessages]
            SET [Status] = {(int)OutboxMessageStatus.Pending}, [AttemptCount] = 0,
                [NextAttemptAtUtc] = {replayedAtUtc}, [LeaseToken] = NULL, [LeaseExpiresAtUtc] = NULL
            WHERE [EventId] = {eventId} AND [Status] = {(int)OutboxMessageStatus.DeadLetter};
            """, cancellationToken);
        return affected == 1;
    }
    public async Task<int> ReapExpiredLeasesAsync(int batchSize, int maxAttempts, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        EnsureUtc(nowUtc, nameof(nowUtc));
        return await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            ;WITH [Candidates] AS
            (
                SELECT TOP ({batchSize}) *
                FROM [dbo].[OutboxMessages] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                WHERE [Status] = {(int)OutboxMessageStatus.Processing}
                    AND [LeaseExpiresAtUtc] <= {nowUtc}
                    AND [AttemptCount] >= {maxAttempts}
                ORDER BY [LeaseExpiresAtUtc], [OccurredAtUtc], [EventId]
            )
            UPDATE [Candidates]
            SET [Status] = {(int)OutboxMessageStatus.DeadLetter},
                [DeadLetterAtUtc] = {nowUtc},
                [LastError] = N'Lease expired after reaching the maximum attempt count.',
                [LeaseToken] = NULL,
                [LeaseExpiresAtUtc] = NULL;
            """, cancellationToken);
    }
    private static void ValidateClaimArguments(int batchSize, DateTimeOffset nowUtc, DateTimeOffset leaseExpiresAtUtc, int maxAttempts)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        EnsureUtc(nowUtc, nameof(nowUtc)); EnsureUtc(leaseExpiresAtUtc, nameof(leaseExpiresAtUtc));
        if (leaseExpiresAtUtc <= nowUtc) throw new ArgumentException("Lease expiration must be later than claim time.", nameof(leaseExpiresAtUtc));
    }
    private static void ValidateLease(Guid eventId, Guid leaseToken, DateTimeOffset timestamp)
    {
        ValidateIds(eventId, leaseToken); EnsureUtc(timestamp, nameof(timestamp));
    }
    private static void ValidateIds(Guid eventId, Guid leaseToken)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        if (leaseToken == Guid.Empty) throw new ArgumentException("LeaseToken is required.", nameof(leaseToken));
    }
    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", name);
    }
    private static string NormalizeError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return error.Length <= 4000 ? error : error[..4000];
    }
}