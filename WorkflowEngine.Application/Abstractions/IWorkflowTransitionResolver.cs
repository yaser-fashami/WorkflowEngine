using Workflow.Application.Services;
using Workflow.Domain.Entities;

namespace Workflow.Application.Abstractions;

//این Component تصمیم می‌گیرد بعد از یک Action، Workflow به کدام Node برود.
public interface IWorkflowTransitionResolver
{
    Task<WorkflowTransition?> ResolveAsync(
        WorkflowNode currentNode,
        string? action,
        WorkflowContext context,
        CancellationToken cancellationToken);
}