using System.Text.Json;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Entities;

namespace Workflow.Infrastructure.Persistence.Outbox;

public sealed class WorkflowOutboxRepository : IWorkflowOutbox
{
    private const string WorkflowCompletedEventType = "workflow.completed.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WorkflowDbContext _dbContext;

    public WorkflowOutboxRepository(WorkflowDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(WorkflowCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var payload = JsonSerializer.Serialize(integrationEvent, JsonOptions);
        var message = OutboxMessage.Create(integrationEvent.EventId, WorkflowCompletedEventType,
            payload, integrationEvent.CompletedAtUtc);
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
