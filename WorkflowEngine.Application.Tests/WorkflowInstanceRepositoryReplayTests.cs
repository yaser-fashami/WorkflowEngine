using Microsoft.EntityFrameworkCore;
using Workflow.Application.Mappers;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WorkflowEngine.Application.Tests;

public sealed class WorkflowInstanceRepositoryReplayTests
{
    [Fact]
    public async Task GetByIdempotencyKeyAsync_LoadsTasksAndAssignmentsForReplayDto()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        const string businessType = "EmploymentContract";
        const string businessReference = "contract-125";
        const string idempotencyKey = "request-abc";
        Guid instanceId;

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

        var replayDto = replayed.ToDto();
        var replayedTask = Assert.Single(replayDto.Tasks);
        Assert.Equal("HR", replayedTask.AssignedRole);
    }
}
