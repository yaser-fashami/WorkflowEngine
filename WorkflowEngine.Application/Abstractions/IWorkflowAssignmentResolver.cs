using Workflow.Domain.ValueObjects;
using Workflow.Application.Services;
//وقتی به Approval Node رسیدیم باید مشخص کنیم چه کسی Task را انجام دهد.
public interface IWorkflowAssignmentResolver
{
    Task<IReadOnlyCollection<WorkflowAssignmentResult>>
        ResolveAsync(
            WorkflowNode node,
            WorkflowContext context,
            CancellationToken cancellationToken);
}