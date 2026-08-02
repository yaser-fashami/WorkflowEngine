
using Workflow.Domain.ValueObjects;

namespace Workflow.Application.Services;

public sealed class WorkflowAssignmentResolver : IWorkflowAssignmentResolver
{
    public Task<IReadOnlyCollection<WorkflowAssignmentResult>>
        ResolveAsync(
            WorkflowNode node,
            WorkflowContext context,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = node.Assignments
                .Select(
                    assignment =>
                        new WorkflowAssignmentResult
                        {
                            Type =
                                assignment.Type,

                            UserId =
                                assignment.UserId,

                            Role =
                                assignment.Role,

                            DepartmentId =
                                assignment.DepartmentId
                        })
                .ToList();


        return Task.FromResult<IReadOnlyCollection<WorkflowAssignmentResult>>(results);
    }
}