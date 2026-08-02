namespace Workflow.Domain.ValueObjects;

using Workflow.Domain.Enums;

public sealed record WorkflowActor(
    WorkflowTaskAssignmentType Type,
    Guid? UserId = null,
    string? Role = null,
    Guid? DepartmentId = null)
{
    public static WorkflowActor ForUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));

        return new WorkflowActor(
            WorkflowTaskAssignmentType.User,
            UserId: userId);
    }

    public static WorkflowActor ForRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException(
                "Role is required.",
                nameof(role));

        return new WorkflowActor(
            WorkflowTaskAssignmentType.Role,
            Role: role);
    }

    public static WorkflowActor ForDepartment(
        Guid departmentId)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException(
                "Department id is required.",
                nameof(departmentId));

        return new WorkflowActor(
            WorkflowTaskAssignmentType.Department,
            DepartmentId: departmentId);
    }
}
