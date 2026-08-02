using Workflow.Application.Services;

namespace Workflow.Application.Abstractions;

public interface IWorkflowConditionEvaluator
{
    Task<bool> EvaluateAsync(
        string expression,
        WorkflowContext context,
        CancellationToken cancellationToken);
}
