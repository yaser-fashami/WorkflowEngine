using Workflow.Domain.Common;
using Workflow.Domain.Enums;

namespace Workflow.Domain.Entities;

public sealed class WorkflowInstance : BaseEntity
{
    private readonly List<WorkflowTask> _tasks = [];

    private WorkflowInstance()
    {
        // Required by EF Core
    }

    private WorkflowInstance(
        Guid id,
        Guid workflowDefinitionId,
        Guid workflowVersionId)
        : base(id)
    {
        if (workflowDefinitionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow definition id is required.",
                nameof(workflowDefinitionId));
        }

        if (workflowVersionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow version id is required.",
                nameof(workflowVersionId));
        }

        WorkflowDefinitionId =
            workflowDefinitionId;

        WorkflowVersionId =
            workflowVersionId;

        Status =
            WorkflowInstanceStatus.Running;

        StartedAt =
            DateTimeOffset.UtcNow;
    }

    public Guid WorkflowDefinitionId
    {
        get;
        private set;
    }

    public Guid WorkflowVersionId
    {
        get;
        private set;
    }

    public Guid? CurrentNodeId
    {
        get;
        private set;
    }

    public WorkflowInstanceStatus Status
    {
        get;
        private set;
    }

    public DateTimeOffset StartedAt
    {
        get;
        private set;
    }

    public DateTimeOffset? CompletedAt
    {
        get;
        private set;
    }

    public Guid? CompletedBy
    {
        get;
        private set;
    }

    public Guid? StartedBy
    {
        get;
        private set;
    }

    public IReadOnlyCollection<WorkflowTask> Tasks =>
        _tasks.AsReadOnly();

    public static WorkflowInstance Create(
        Guid workflowDefinitionId,
        Guid workflowVersionId)
    {
        return new WorkflowInstance(
            Guid.NewGuid(),
            workflowDefinitionId,
            workflowVersionId);
    }

    public void AddTask(
        WorkflowTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (Status !=
            WorkflowInstanceStatus.Running)
        {
            throw new InvalidOperationException(
                "Tasks can only be added to " +
                "a running workflow instance.");
        }

        if (task.WorkflowInstanceId != Id)
        {
            throw new InvalidOperationException(
                "Task does not belong to " +
                "this workflow instance.");
        }

        if (_tasks.Any(x =>
            x.Id == task.Id))
        {
            return;
        }

        _tasks.Add(task);
    }

    public WorkflowTask AddUserTask(
        Guid nodeId,
        WorkflowTaskType taskType,
        Guid userId)
    {
        var task =
            WorkflowTask.Create(
                Id,
                nodeId,
                taskType);

        task.AssignToUser(
            userId);

        AddTask(task);

        return task;
    }

    public WorkflowTask AddRoleTask(
        Guid nodeId,
        WorkflowTaskType taskType,
        string role)
    {
        var task =
            WorkflowTask.Create(
                Id,
                nodeId,
                taskType);

        task.AssignToRole(
            role);

        AddTask(task);

        return task;
    }

    public WorkflowTask AddDepartmentTask(
        Guid nodeId,
        WorkflowTaskType taskType,
        Guid departmentId)
    {
        var task =
            WorkflowTask.Create(
                Id,
                nodeId,
                taskType);

        task.AssignToDepartment(
            departmentId);

        AddTask(task);

        return task;
    }

    public void Start(Guid startedBy)
    {
        if (startedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "Started by user is required.",
                nameof(startedBy));
        }

        if (Status != WorkflowInstanceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Workflow instance cannot be started " +
                $"because its current status is " +
                $"'{Status}'.");
        }

        Status =
            WorkflowInstanceStatus.Running;

        StartedAt =
            DateTimeOffset.UtcNow;

        StartedBy =
            startedBy;
    }

    public void MoveTo(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Node id is required.",
                nameof(nodeId));
        }

        CurrentNodeId = nodeId;
    }

    public void Complete(
        Guid performedBy)
    {
        if (Status !=
            WorkflowInstanceStatus.Running)
        {
            throw new InvalidOperationException(
                "Only running workflow instances " +
                "can be completed.");
        }

        Status =
            WorkflowInstanceStatus.Completed;

        CompletedBy =
            performedBy;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }
}