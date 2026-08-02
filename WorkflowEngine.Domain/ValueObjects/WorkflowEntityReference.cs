namespace Workflow.Domain.ValueObjects;

public sealed record WorkflowEntityReference(
    string EntityType,
    Guid EntityId)
{
    public static WorkflowEntityReference Create(
        string entityType,
        Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException(
                "Entity type is required.",
                nameof(entityType));

        if (entityId == Guid.Empty)
            throw new ArgumentException(
                "Entity id is required.",
                nameof(entityId));

        return new WorkflowEntityReference(
            entityType,
            entityId);
    }
}