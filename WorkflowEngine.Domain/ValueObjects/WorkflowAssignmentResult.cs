using Workflow.Domain.Enums;

namespace Workflow.Domain.ValueObjects;

public sealed class WorkflowAssignmentResult
{
    public WorkflowAssignmentType Type { get; init; }

    public Guid? UserId { get; init; }

    public string? Role { get; init; }

    public Guid? DepartmentId { get; init; }
}