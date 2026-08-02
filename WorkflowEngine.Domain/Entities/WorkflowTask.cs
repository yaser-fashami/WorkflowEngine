using Workflow.Domain.Common;
using Workflow.Domain.Enums;

namespace Workflow.Domain.Entities;

public sealed class WorkflowTask : BaseEntity
{
    private readonly List<WorkflowTaskAssignment>
        _assignments = [];

    private WorkflowTask()
    {
        // Required by EF Core
    }

    private WorkflowTask(
        Guid id,
        Guid workflowInstanceId,
        Guid workflowNodeId,
        WorkflowTaskType taskType)
        : base(id)
    {
        if (workflowInstanceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow instance id is required.",
                nameof(workflowInstanceId));
        }

        if (workflowNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow node id is required.",
                nameof(workflowNodeId));
        }

        WorkflowInstanceId =
            workflowInstanceId;

        WorkflowNodeId =
            workflowNodeId;

        TaskType =
            taskType;

        Status =
            WorkflowTaskStatus.Pending;

        CreatedAt =
            DateTimeOffset.UtcNow;
    }

    public Guid WorkflowInstanceId
    {
        get;
        private set;
    }

    public Guid WorkflowNodeId
    {
        get;
        private set;
    }

    public WorkflowTaskType TaskType
    {
        get;
        private set;
    }

    public WorkflowTaskStatus Status
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAt
    {
        get;
        private set;
    }

    public DateTimeOffset? DueDate
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

    public IReadOnlyCollection<WorkflowTaskAssignment> Assignments =>
            _assignments.AsReadOnly();

    public static WorkflowTask Create(
        Guid workflowInstanceId,
        Guid workflowNodeId,
        WorkflowTaskType taskType)
    {
        return new WorkflowTask(
            Guid.NewGuid(),
            workflowInstanceId,
            workflowNodeId,
            taskType);
    }

    public void SetDueDate(
        DateTimeOffset dueDate)
    {
        if (Status !=
            WorkflowTaskStatus.Pending)
        {
            throw new InvalidOperationException(
                "Due date can only be set " +
                "for pending tasks.");
        }

        DueDate =
            dueDate;
    }

    public void AssignToUser(
        Guid userId)
    {
        EnsurePending();

        var assignment =
            WorkflowTaskAssignment.ForUser(
                Id,
                userId);

        AddAssignment(
            assignment);
    }

    public void AssignToRole(
        string role)
    {
        EnsurePending();

        var assignment =
            WorkflowTaskAssignment.ForRole(
                Id,
                role);

        AddAssignment(
            assignment);
    }

    public void AssignToDepartment(
        Guid departmentId)
    {
        EnsurePending();

        var assignment =
            WorkflowTaskAssignment.ForDepartment(
                Id,
                departmentId);

        AddAssignment(
            assignment);
    }

    public void AddAssignment(
        WorkflowTaskAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        EnsurePending();

        if (assignment.WorkflowTaskId != Id)
        {
            throw new InvalidOperationException(
                "Assignment does not belong " +
                "to this workflow task.");
        }

        if (_assignments.Any(x =>
            x.Type == assignment.Type &&
            x.UserId == assignment.UserId &&
            x.Role == assignment.Role &&
            x.DepartmentId ==
                assignment.DepartmentId))
        {
            return;
        }

        _assignments.Add(
            assignment);
    }

    public bool CanBePerformedBy(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        return _assignments.Any(
            x => x.MatchesUser(userId));
    }

    public void Complete(
        Guid performedBy)
    {
        EnsurePending();

        EnsureAssignedUser(
            performedBy);

        Status =
            WorkflowTaskStatus.Completed;

        CompletedBy =
            performedBy;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }

    public void Reject(
        Guid performedBy)
    {
        EnsurePending();

        EnsureAssignedUser(
            performedBy);

        Status =
            WorkflowTaskStatus.Rejected;

        CompletedBy =
            performedBy;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        EnsurePending();

        Status =
            WorkflowTaskStatus.Cancelled;
    }

    public bool IsPending()
    {
        return Status ==
            WorkflowTaskStatus.Pending;
    }

    public bool IsCompleted()
    {
        return Status ==
            WorkflowTaskStatus.Completed;
    }

    public bool IsRejected()
    {
        return Status ==
            WorkflowTaskStatus.Rejected;
    }

    private void EnsurePending()
    {
        if (Status !=
            WorkflowTaskStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending tasks can be modified.");
        }
    }

    private void EnsureAssignedUser(
        Guid performedBy)
    {
        if (performedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "Performed by user is required.",
                nameof(performedBy));
        }

        if (!CanBePerformedBy(
            performedBy))
        {
            throw new InvalidOperationException(
                "The user is not assigned " +
                "to this workflow task.");
        }
    }
}