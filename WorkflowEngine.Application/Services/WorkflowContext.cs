
namespace Workflow.Application.Services;

public sealed class WorkflowContext
{
    private readonly Dictionary<string, object?> _data = [];

    public Guid WorkflowInstanceId { get; }

    public Guid WorkflowDefinitionId { get; }

    public Guid WorkflowVersionId { get; }

    public Guid UserId { get; }

    public WorkflowContext(
        Guid workflowInstanceId,
        Guid workflowDefinitionId,
        Guid workflowVersionId,
        Guid userId)
    {
        WorkflowInstanceId = workflowInstanceId;
        WorkflowDefinitionId = workflowDefinitionId;
        WorkflowVersionId = workflowVersionId;
        UserId = userId;
    }

    public void Set(
        string key,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Context key is required.",
                nameof(key));
        }

        _data[key] = value;
    }

    public object? Get(
        string key)
    {
        return _data.TryGetValue(
            key,
            out var value)
            ? value
            : null;
    }

    public T? Get<T>(
        string key)
    {
        var value = Get(key);

        if (value is null)
        {
            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException(
            $"Context value '{key}' " +
            $"is not of type {typeof(T).Name}.");
    }

    public IReadOnlyDictionary<string, object?>
        Data => _data;
}