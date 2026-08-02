using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Application.Services;

public sealed class WorkflowTaskAuthorizationService : IWorkflowTaskAuthorizationService
{
    public async Task<bool> CanExecuteAsync(
        WorkflowTask task,
        Guid userId,
        CancellationToken cancellationToken)
    {
        foreach (var assignee in task.Assignments)
        {
            switch (assignee.Type)
            {
                case WorkflowTaskAssignmentType.User:

                    if (assignee.UserId == userId)
                    {
                        return true;
                    }

                    break;

                case WorkflowTaskAssignmentType.Role:

                    var hasRole =
                        await UserHasRoleAsync(
                            userId,
                            assignee.Role!,
                            cancellationToken);

                    if (hasRole)
                    {
                        return true;
                    }

                    break;

                case WorkflowTaskAssignmentType.Department:

                    var belongsToDepartment =
                        await UserBelongsToDepartmentAsync(
                            userId,
                            assignee.DepartmentId!.Value,
                            cancellationToken);

                    if (belongsToDepartment)
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private Task<bool> UserHasRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        // Identity / Permission Service
        throw new NotImplementedException();
    }

    private Task<bool> UserBelongsToDepartmentAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        // Organization Service
        throw new NotImplementedException();
    }
}
