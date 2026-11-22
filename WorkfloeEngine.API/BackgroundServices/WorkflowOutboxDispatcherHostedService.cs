using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;

namespace WorkflowEngine.API.BackgroundServices;

/// <summary>
/// Development-only Outbox dispatcher. Production delivery must use the approved OAuth2/JWT client integration.
/// </summary>
public sealed class WorkflowOutboxDispatcherHostedService : BackgroundService
{
    private const string ExpectedEventType = "workflow.completed.v1";
    private static readonly int[] RetryDelaySeconds = [5, 15, 30, 60, 120, 240, 480, 900, 1800];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkflowOutboxDispatcherHostedService> _logger;
    private readonly Uri _callbackUri;
    private readonly string _bearerToken;
    private readonly int _batchSize;
    private readonly int _maxAttempts;
    private readonly TimeSpan _leaseDuration = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _idleDelay = TimeSpan.FromSeconds(5);

    public WorkflowOutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<WorkflowOutboxDispatcherHostedService> logger)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("The test-token Outbox dispatcher may only run in Development.");

        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var section = configuration.GetSection("WorkflowIntegration:OutboxDispatcher");
        var callbackUrl = section["CallbackUrl"];
        _bearerToken = configuration["Authentication:DevelopmentBearerToken"] ?? string.Empty;
        _batchSize = section.GetValue("BatchSize", 20);
        _maxAttempts = section.GetValue("MaxAttempts", 10);

        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out _callbackUri) ||
            (_callbackUri.Scheme != Uri.UriSchemeHttps && _callbackUri.Scheme != Uri.UriSchemeHttp))
            throw new InvalidOperationException("A valid WorkflowIntegration:OutboxDispatcher:CallbackUrl is required in Development.");

        if (string.IsNullOrWhiteSpace(_bearerToken))
            throw new InvalidOperationException("Authentication:DevelopmentBearerToken must be provided through a local secret or environment variable.");

        if (_batchSize <= 0 || _maxAttempts <= 0)
            throw new InvalidOperationException("Outbox dispatcher BatchSize and MaxAttempts must be greater than zero.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("Development-only Workflow Outbox dispatcher is enabled. Do not use this shared-token mode in Production.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimed = await ClaimBatchAsync(stoppingToken);
                if (claimed.Count == 0)
                {
                    await Task.Delay(_idleDelay, stoppingToken);
                    continue;
                }

                await Task.WhenAll(claimed.Select(message => DispatchOneAsync(message, stoppingToken)));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Workflow Outbox dispatcher iteration failed. It will retry after the idle delay.");
                await Task.Delay(_idleDelay, stoppingToken);
            }
        }
    }

    private async Task<IReadOnlyList<OutboxMessageClaim>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowOutboxClaimRepository>();
        var now = DateTimeOffset.UtcNow;

        return await repository.ClaimBatchAsync(
            _batchSize,
            now,
            now.Add(_leaseDuration),
            _maxAttempts,
            cancellationToken);
    }

    private async Task DispatchOneAsync(OutboxMessageClaim message, CancellationToken cancellationToken)
    {
        if (!IsValidEvent(message))
        {
            await MoveToDeadLetterAsync(message, "Unsupported event type or payload EventId mismatch.", cancellationToken);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _callbackUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            request.Content = new StringContent(message.Payload, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("WorkflowCallback");
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var statusCode = (int)response.StatusCode;
            if (statusCode is StatusCodes.Status200OK or StatusCodes.Status202Accepted)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IWorkflowOutboxClaimRepository>();
                var updated = await repository.MarkPublishedAsync(
                    message.EventId,
                    message.LeaseToken,
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                if (!updated)
                    _logger.LogWarning("Outbox EventId {EventId} was accepted remotely, but its lease is no longer current. A duplicate delivery may occur.", message.EventId);
                return;
            }

            var error = $"Workflow callback returned HTTP {statusCode} ({response.ReasonPhrase}).";

            if (statusCode is StatusCodes.Status400BadRequest or StatusCodes.Status401Unauthorized
                or StatusCodes.Status403Forbidden or StatusCodes.Status409Conflict
                || (statusCode >= 400 && statusCode < 500 && statusCode is not StatusCodes.Status408RequestTimeout
                    and not StatusCodes.Status429TooManyRequests)
                || statusCode is >= 300 and < 400)
            {
                await MoveToDeadLetterAsync(message, error, cancellationToken);
                _logger.LogError("Outbox EventId {EventId} moved to DeadLetter after permanent callback response {StatusCode}.", message.EventId, statusCode);
                return;
            }

            if (statusCode == StatusCodes.Status408RequestTimeout ||
                statusCode == StatusCodes.Status429TooManyRequests ||
                statusCode >= 500)
            {
                var retryAt = CalculateNextAttempt(message.AttemptCount, response.Headers.RetryAfter);
                await ScheduleRetryAsync(message, retryAt, error, cancellationToken);
                return;
            }

            await MoveToDeadLetterAsync(message, $"Unexpected callback response: {error}", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leave the lease to expire. The next worker can safely reclaim it.
        }
        catch (Exception exception)
        {
            var retryAt = CalculateNextAttempt(message.AttemptCount, retryAfter: null);
            await ScheduleRetryAsync(message, retryAt, $"{exception.GetType().Name}: {exception.Message}", cancellationToken);
        }
    }

    private static bool IsValidEvent(OutboxMessageClaim message)
    {
        if (!string.Equals(message.EventType, ExpectedEventType, StringComparison.Ordinal))
            return false;

        try
        {
            using var document = JsonDocument.Parse(message.Payload);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("eventId", out var eventId) &&
                   eventId.ValueKind == JsonValueKind.String &&
                   Guid.TryParse(eventId.GetString(), out var parsedId) &&
                   parsedId == message.EventId;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DateTimeOffset CalculateNextAttempt(int attemptCount, RetryConditionHeaderValue? retryAfter)
    {
        var index = Math.Clamp(attemptCount, 1, RetryDelaySeconds.Length) - 1;
        var baseSeconds = RetryDelaySeconds[index];
        var jitteredSeconds = baseSeconds * (0.8 + Random.Shared.NextDouble() * 0.4);
        var delay = TimeSpan.FromSeconds(jitteredSeconds);

        if (retryAfter?.Delta is { } delta && delta > delay)
            delay = delta;
        else if (retryAfter?.Date is { } date)
        {
            var dateDelay = date - DateTimeOffset.UtcNow;
            if (dateDelay > delay)
                delay = dateDelay;
        }

        return DateTimeOffset.UtcNow.Add(delay);
    }

    private async Task ScheduleRetryAsync(
        OutboxMessageClaim message,
        DateTimeOffset nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowOutboxClaimRepository>();
        var updated = await repository.ScheduleRetryAsync(
            message.EventId,
            message.LeaseToken,
            nextAttemptAtUtc,
            error,
            _maxAttempts,
            cancellationToken);

        if (!updated)
            _logger.LogWarning("Outbox EventId {EventId} retry state was not updated because its lease is no longer current.", message.EventId);
    }

    private async Task MoveToDeadLetterAsync(
        OutboxMessageClaim message,
        string error,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowOutboxClaimRepository>();
        var updated = await repository.MoveToDeadLetterAsync(
            message.EventId,
            message.LeaseToken,
            error,
            cancellationToken);

        if (!updated)
            _logger.LogWarning("Outbox EventId {EventId} could not be moved to DeadLetter because its lease is no longer current.", message.EventId);
    }
}
