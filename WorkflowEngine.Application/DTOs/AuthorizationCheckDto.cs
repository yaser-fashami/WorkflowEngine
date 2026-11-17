namespace Workflow.Application.DTOs;

/// <summary>
/// Result of checking whether a user may execute an action on a workflow task.
/// </summary>
public sealed record AuthorizationCheckDto(
    bool IsAllowed,
    string ReasonCode,
    string Message);
