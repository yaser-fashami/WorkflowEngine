using Workflow.Domain.Common;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

public sealed class WorkflowNode : BaseEntity
{
    private readonly List<WorkflowTransition> _outgoingTransitions = [];

    private readonly List<WorkflowNodeAssignment> _assignments = [];

    private WorkflowNode()
    {
    }

    private WorkflowNode(
        Guid id,
        Guid workflowVersionId,
        string code,
        string name,
        WorkflowNodeType type)
        : base(id)
    {
        WorkflowVersionId = workflowVersionId;
        Code = code;
        Name = name;
        Type = type;

        CreatedAt =
            DateTimeOffset.UtcNow;
    }

    public Guid WorkflowVersionId
    {
        get;
        private set;
    }

    public string Code
    {
        get;
        private set;
    } = null!;

    public string Name
    {
        get;
        private set;
    } = null!;

    public WorkflowNodeType Type
    {
        get;
        private set;
    }

    public IReadOnlyCollection<WorkflowTransition> OutgoingTransitions
        => _outgoingTransitions.AsReadOnly();

    public IReadOnlyCollection<WorkflowNodeAssignment> Assignments
        => _assignments.AsReadOnly();

    public static WorkflowNode Create(
        Guid workflowVersionId,
        string code,
        string name,
        WorkflowNodeType type)
    {
        return new WorkflowNode(
            Guid.NewGuid(),
            workflowVersionId,
            code,
            name,
            type);
    }

    public WorkflowTransition AddTransition(
        Guid targetNodeId,
        string? action = null)
    {
        if (Type == WorkflowNodeType.End)
        {
            throw new InvalidOperationException(
                "End node cannot have outgoing transitions.");
        }

        var transition =
            WorkflowTransition.Create(
                Id,
                targetNodeId,
                action);

        _outgoingTransitions.Add(
            transition);

        MarkAsModified();

        return transition;
    }

    public WorkflowNodeAssignment AddUserAssignment(
        Guid userId)
    {
        EnsureCanHaveAssignment();

        var assignment =
            WorkflowNodeAssignment.CreateForUser(
                Id,
                userId);

        _assignments.Add(
            assignment);

        MarkAsModified();

        return assignment;
    }

    public WorkflowNodeAssignment AddRoleAssignment(
        string role)
    {
        EnsureCanHaveAssignment();

        var assignment =
            WorkflowNodeAssignment.CreateForRole(
                Id,
                role);

        _assignments.Add(
            assignment);

        MarkAsModified();

        return assignment;
    }

    public WorkflowNodeAssignment AddDepartmentAssignment(
        Guid departmentId)
    {
        EnsureCanHaveAssignment();

        var assignment =
            WorkflowNodeAssignment.CreateForDepartment(
                Id,
                departmentId);

        _assignments.Add(
            assignment);

        MarkAsModified();

        return assignment;
    }

    public void RemoveAssignment(
        Guid assignmentId)
    {
        var assignment =
            _assignments.FirstOrDefault(
                x => x.Id == assignmentId);

        if (assignment is null)
        {
            return;
        }

        _assignments.Remove(
            assignment);

        MarkAsModified();
    }

    private void EnsureCanHaveAssignment()
    {
        if (Type is
            WorkflowNodeType.Start or
            WorkflowNodeType.End)
        {
            throw new InvalidOperationException(
                $"Node type '{Type}' " +
                "cannot have assignments.");
        }
    }
}