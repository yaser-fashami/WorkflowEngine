using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WorkflowEngine.Application.Tests;

public sealed class WorkflowInstanceRepositoryReplayTests
{
    [Fact]
    public async Task GetByIdempotencyKeyAsync_IncludesTasksAndAssignments()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var businessType = "EmploymentContract";
        var businessReference = "contract-125";
        var idempotencyKey = "request-abc";
        var instanceId = Guid.NewGuid();

        await using (var writeContext = new WorkflowDbContext(options))
        {
            var instance = WorkflowInstance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                businessType,
                businessReference,
                "trace-001",
                idempotencyKey,
                1,
                "{}",
                new string('A', 64));

            instance.Start(Guid.NewGuid());

            var task = WorkflowTask.Create(
                instance.Id,
                Guid.NewGuid(),
                WorkflowTaskType.UserTask);
            task.AssignToRole("HR");
            instance.AddTask(task);

            var repository = new WorkflowInstanceRepository(writeContext);
            await repository.AddAsync(instance);
            await writeContext.SaveChangesAsync();

            instanceId = instance.Id;
        }

        await using var readContext = new WorkflowDbContext(options);
        var readRepository = new WorkflowInstanceRepository(readContext);

        var replayed = await readRepository.GetByIdempotencyKeyAsync(
            businessType,
            businessReference,
            idempotencyKey);

        Assert.NotNull(replayed);
        Assert.Equal(instanceId, replayed.Id);

        var replayedTask = Assert.Single(replayed.Tasks);
        var replayedAssignment = Assert.Single(replayedTask.Assignments);
        Assert.Equal("HR", replayedAssignment.Role);
    }
}
