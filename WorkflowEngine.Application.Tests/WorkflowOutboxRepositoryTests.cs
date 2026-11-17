using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.Application.DTOs;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Outbox;
using Xunit;

namespace WorkflowEngine.Application.Tests;

public sealed class WorkflowOutboxRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsWorkflowCompletedEventPayload()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new WorkflowDbContext(options);
        var repository = new WorkflowOutboxRepository(dbContext);
        var integrationEvent = new WorkflowCompletedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            BusinessType = "EmploymentContract",
            BusinessReference = "contract-123",
            CompletedAtUtc = DateTimeOffset.UtcNow,
            DocumentId = "document-123",
            DocumentVersion = "v3",
            DocumentHash = "sha256:example"
        };

        await repository.AddAsync(integrationEvent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var message = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(integrationEvent.EventId, message.EventId);
        Assert.Equal("workflow.completed.v1", message.EventType);
        Assert.Equal(0, message.AttemptCount);
        Assert.Equal(0, (int)message.Status);

        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal(
            integrationEvent.WorkflowInstanceId,
            payload.RootElement.GetProperty("workflowInstanceId").GetGuid());
        Assert.Equal(
            integrationEvent.DocumentHash,
            payload.RootElement.GetProperty("documentHash").GetString());
    }
}
