namespace Workflow.Api.Contracts;

/// <summary>Response returned by the WorkflowEngine API availability endpoint.</summary>
public sealed class PingResponse
{
    /// <summary>Human-readable API availability message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Current UTC server timestamp.</summary>
    public DateTime Time { get; init; }
}
