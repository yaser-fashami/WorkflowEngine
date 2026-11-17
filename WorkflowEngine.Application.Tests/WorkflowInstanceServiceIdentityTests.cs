using Workflow.Application.Mappers;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Xunit;

namespace WorkflowEngine.Application.Tests;

public sealed class WorkflowInstanceServiceIdentityTests
{
    private static WorkflowInstance CreateInstance() =>
        WorkflowInstance.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Test", "reference-1", "trace-1",
            "key-1", 1, "{}", new string('A', 64));

    [Fact]
    public void Start_StoresServiceId_AndDoesNotPopulateLegacyStartedBy()
    {
        var instance = CreateInstance();

        instance.Start("sinahrm-api");

        Assert.Equal("sinahrm-api", instance.StartedByService);
        Assert.Null(instance.StartedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("SinaHRM-API")]
    [InlineData("sina hrm")]
    [InlineData("-sinahrm")]
    [InlineData("sinahrm-")]
    public void Start_RejectsInvalidServiceId(string serviceId)
    {
        var instance = CreateInstance();

        Assert.Throws<ArgumentException>(() => instance.Start(serviceId));
    }

    [Fact]
    public void Start_RejectsServiceIdLongerThan100Characters()
    {
        var instance = CreateInstance();

        Assert.Throws<ArgumentException>(() => instance.Start(new string('a', 101)));
    }

    [Fact]
    public void CompleteAutomatically_LeavesCompletedByNull_AndMapperHandlesNull()
    {
        var instance = CreateInstance();
        instance.Start("sinahrm-api");

        instance.CompleteAutomatically();

        Assert.Equal(WorkflowInstanceStatus.Completed, instance.Status);
        Assert.Null(instance.CompletedBy);
        Assert.NotNull(instance.CompletedAt);
        Assert.Null(instance.ToDto().CompletedBy);
    }

    [Fact]
    public void Complete_RecordsHumanActor()
    {
        var instance = CreateInstance();
        var performedBy = Guid.NewGuid();
        instance.Start("sinahrm-api");

        instance.Complete(performedBy);

        Assert.Equal(performedBy, instance.CompletedBy);
    }
}
