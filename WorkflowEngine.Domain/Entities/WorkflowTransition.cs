namespace Workflow.Domain.Entities;

using Workflow.Domain.Common;

public sealed class WorkflowTransition : BaseEntity
{
    private WorkflowTransition()
    {
    }

    private WorkflowTransition(
        Guid id,
        Guid sourceNodeId,
        Guid targetNodeId,
        string? action)
        : base(id)
    {
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        Action = action;

        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid SourceNodeId { get; private set; }

    public Guid TargetNodeId { get; private set; }

    public string? Action { get; private set; }

    public string? ConditionExpression
    {
        get;
        private set;
    }

    public static WorkflowTransition Create(
        Guid sourceNodeId,
        Guid targetNodeId,
        string? action)
    {
        if (sourceNodeId == targetNodeId)
            throw new InvalidOperationException(
                "Self transition is not allowed.");

        return new WorkflowTransition(
            Guid.NewGuid(),
            sourceNodeId,
            targetNodeId,
            action);
    }

    public void SetCondition(
        string conditionExpression)
    {
        if (string.IsNullOrWhiteSpace(
            conditionExpression))
        {
            throw new ArgumentException(
                "Condition expression is required.",
                nameof(conditionExpression));
        }

        ConditionExpression =
            conditionExpression;

        MarkAsModified();
    }
}
