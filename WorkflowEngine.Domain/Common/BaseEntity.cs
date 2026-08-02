namespace Workflow.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity()
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; protected init; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public DateTimeOffset? ModifiedAt { get; protected set; }

    protected void MarkAsModified()
    {
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}