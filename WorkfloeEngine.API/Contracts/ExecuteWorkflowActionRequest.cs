namespace Workflow.Api.Contracts;

public sealed class ExecuteWorkflowActionRequest
{
    public string Action
    {
        get;
        set;
    } = string.Empty;
}