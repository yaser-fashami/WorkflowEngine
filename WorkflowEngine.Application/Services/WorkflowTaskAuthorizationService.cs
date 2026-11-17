using Workflow.Application.Abstractions;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Application.Services;

/// <summary>
/// Evaluates only User assignments in the first authorization phase.
/// Role and Department assignments deliberately do not grant access.
/// </summary>
public sealed class WorkflowTaskAuthorizationService : IWorkflowTaskAuthorizationService
{
    public Task<bool> CanExecuteAsync(
        WorkflowTask task,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty || task.Status != WorkflowTaskStatus.Pending)
            return Task.FromResult(false);

        var isAssignedUser = task.Assignments.Any(assignment =>
            assignment.Type == WorkflowTaskAssignmentType.User &&
            assignment.UserId == userId);

        return Task.FromResult(isAssignedUser);
    }
}
