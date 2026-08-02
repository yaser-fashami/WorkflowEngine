using Workflow.Domain.Common;
using Workflow.Domain.Enums;

namespace Workflow.Domain.Entities;

public sealed class WorkflowTaskAssignment : BaseEntity
{
    private WorkflowTaskAssignment()
    {
        // Required by EF Core
    }

    private WorkflowTaskAssignment(
        Guid id,
        Guid workflowTaskId,
        WorkflowTaskAssignmentType type)
        : base(id)
    {
        if (workflowTaskId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow task id is required.",
                nameof(workflowTaskId));
        }

        WorkflowTaskId =
            workflowTaskId;

        Type =
            type;
    }

    public Guid WorkflowTaskId
    {
        get;
        private set;
    }

    public WorkflowTaskAssignmentType Type
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

    public static WorkflowTaskAssignment ForUser(
        Guid workflowTaskId,
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        var assignment =
            new WorkflowTaskAssignment(
                Guid.NewGuid(),
                workflowTaskId,
                WorkflowTaskAssignmentType.User);

        assignment.UserId =
            userId;

        return assignment;
    }

    public static WorkflowTaskAssignment ForRole(
        Guid workflowTaskId,
        string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException(
                "Role is required.",
                nameof(role));
        }

        var assignment =
            new WorkflowTaskAssignment(
                Guid.NewGuid(),
                workflowTaskId,
                WorkflowTaskAssignmentType.Role);

        assignment.Role =
            role;

        return assignment;
    }

    public static WorkflowTaskAssignment ForDepartment(
        Guid workflowTaskId,
        Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Department id is required.",
                nameof(departmentId));
        }

        var assignment =
            new WorkflowTaskAssignment(
                Guid.NewGuid(),
                workflowTaskId,
                WorkflowTaskAssignmentType.Department);

        assignment.DepartmentId =
            departmentId;

        return assignment;
    }

    public bool MatchesUser(
        Guid userId)
    {
        return Type ==
                   WorkflowTaskAssignmentType.User
               &&
               UserId == userId;
    }
}
