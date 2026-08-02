namespace Workflow.Domain.Entities;

using Workflow.Domain.Common;

public sealed class WorkflowHistory : BaseEntity
{
    private WorkflowHistory()
    {
    }

    private WorkflowHistory(
        Guid id,
        Guid workflowInstanceId,
        Guid? nodeId,
        string action,
        Guid performedBy)
        : base(id)
    {
        WorkflowInstanceId = workflowInstanceId;
        NodeId = nodeId;
        Action = action;
        PerformedBy = performedBy;

        OccurredAt =
            DateTimeOffset.UtcNow;

        CreatedAt = OccurredAt;
    }

    public Guid WorkflowInstanceId
    {
        get;
        private set;
    }

    public Guid? NodeId
    {
        get;
        private set;
    }

    public string Action
    {
        get;
        private set;
    } = null!;

    public Guid PerformedBy
    {
        get;
        private set;
    }

    public DateTimeOffset OccurredAt
    {
        get;
        private set;
    }

    public static WorkflowHistory Create(
        Guid workflowInstanceId,
        Guid? nodeId,
        string action,
        Guid performedBy)
    {
        return new WorkflowHistory(
            Guid.NewGuid(),
            workflowInstanceId,
            nodeId,
            action,
            performedBy);
    }
}
