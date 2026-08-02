namespace Workflow.Api.Contracts;

public sealed class StartWorkflowRequest
{
    public Guid WorkflowDefinitionId
    {
        get;
        set;
    }

    public object? Input
    {
        get;
        set;
    }
}
