using Workflow.Application.Abstractions;
using Workflow.Domain.Entities;

namespace Workflow.Application.Services;

public sealed class WorkflowTransitionResolver : IWorkflowTransitionResolver
{
    private readonly IWorkflowConditionEvaluator _conditionEvaluator;

    public WorkflowTransitionResolver(IWorkflowConditionEvaluator conditionEvaluator)
    {
        _conditionEvaluator =
            conditionEvaluator;
    }

    public async Task<WorkflowTransition?>
        ResolveAsync(
            WorkflowNode currentNode,
            string? action,
            WorkflowContext context,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentNode);

        var transitions =
            currentNode.OutgoingTransitions;

        // اگر Action مشخص شده، فقط Transitionهای
        // مربوط به همان Action را بررسی می‌کنیم.
        if (!string.IsNullOrWhiteSpace(action))
        {
            transitions =
                transitions
                    .Where(x =>
                        string.Equals(
                            x.Action,
                            action,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        // Transitionها را به ترتیب بررسی می‌کنیم.
        foreach (var transition in transitions)
        {
            // اگر شرطی ندارد، Transition معتبر است.
            if (string.IsNullOrWhiteSpace(
                transition.ConditionExpression))
            {
                return transition;
            }

            // اگر شرط دارد، باید Evaluate شود.
            var conditionResult =
                await _conditionEvaluator.EvaluateAsync(
                    transition.ConditionExpression,
                    context,
                    cancellationToken);

            if (conditionResult)
            {
                return transition;
            }
        }

        return null;
    }

}