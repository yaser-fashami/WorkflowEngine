namespace Workflow.Application.DTOs;

public sealed class WorkflowTaskDto
{
    public Guid Id
    {
        get;
        set;
    }

    public Guid WorkflowInstanceId
    {
        get;
        set;
    }

    public Guid NodeId
    {
        get;
        set;
    }

    public string? NodeCode
    {
        get;
        set;
    }

    public string? NodeName
    {
        get;
        set;
    }

    public string Type
    {
        get;
        set;
    } = string.Empty;

    public string Status
    {
        get;
        set;
    } = string.Empty;

    public string? AssignmentType
    {
        get;
        set;
    }

    public Guid? AssignedUserId
    {
        get;
        set;
    }

    public string? AssignedRole
    {
        get;
        set;
    }

    public Guid? AssignedDepartmentId
    {
        get;
        set;
    }

    public DateTimeOffset? DueDate
    {
        get;
        set;
    }

    public DateTimeOffset? CompletedAt
    {
        get;
        set;
    }

    public Guid? CompletedBy
    {
        get;
        set;
    }
}