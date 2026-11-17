using Workflow.Application.DTOs;

namespace Workflow.Application.DTOs;

public sealed class WorkflowInstanceDto
{
    public Guid Id
    {
        get;
        set;
    }

    public Guid WorkflowDefinitionId
    {
        get;
        set;
    }

    public Guid WorkflowVersionId
    {
        get;
        set;
    }

    public string Status
    {
        get;
        set;
    } = string.Empty;

    public Guid? StartedBy
    {
        get;
        set;
    }

    public string? StartedByService { get; set; }

    public DateTimeOffset? StartedAt
    {
        get;
        set;
    }

    public Guid? CompletedBy
    {
        get;
        set;
    }

    public DateTimeOffset? CompletedAt
    {
        get;
        set;
    }

    public string? DocumentId { get; set; }
    public string? DocumentVersion { get; set; }
    public string? DocumentHash { get; set; }

    public IReadOnlyCollection<WorkflowTaskDto> Tasks
    {
        get;
        set;
    } = [];
}