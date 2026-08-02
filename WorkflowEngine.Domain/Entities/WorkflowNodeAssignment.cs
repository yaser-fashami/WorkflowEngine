using Workflow.Domain.Common;
using Workflow.Domain.Enums;

public sealed class WorkflowNodeAssignment : BaseEntity
{
    private WorkflowNodeAssignment()
    {
    }

    private WorkflowNodeAssignment(
        Guid id,
        Guid workflowNodeId,
        WorkflowAssignmentType type,
        Guid? userId,
        string? role,
        Guid? departmentId)
        : base(id)
    {
        WorkflowNodeId =
            workflowNodeId;

        Type =
            type;

        UserId =
            userId;

        Role =
            role;

        DepartmentId =
            departmentId;

        CreatedAt =
            DateTimeOffset.UtcNow;
    }

    public Guid WorkflowNodeId
    {
        get;
        private set;
    }

    public WorkflowAssignmentType Type
    {
        get;
        private set;
    }

    public Guid? UserId
    {
        get;
        private set;
    }

    public string? Role
    {
        get;
        private set;
    }

    public Guid? DepartmentId
    {
        get;
        private set;
    }

    public static WorkflowNodeAssignment CreateForUser(
        Guid workflowNodeId,
        Guid userId)
    {
        return new WorkflowNodeAssignment(
            Guid.NewGuid(),
            workflowNodeId,
            WorkflowAssignmentType.User,
            userId,
            null,
            null);
    }

    public static WorkflowNodeAssignment CreateForRole(
        Guid workflowNodeId,
        string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException(
                "Role is required.",
                nameof(role));
        }

        return new WorkflowNodeAssignment(
            Guid.NewGuid(),
            workflowNodeId,
            WorkflowAssignmentType.Role,
            null,
            role,
            null);
    }

    public static WorkflowNodeAssignment
        CreateForDepartment(
            Guid workflowNodeId,
            Guid departmentId)
    {
        return new WorkflowNodeAssignment(
            Guid.NewGuid(),
            workflowNodeId,
            WorkflowAssignmentType.Department,
            null,
            null,
            departmentId);
    }
}
