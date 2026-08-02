namespace Workflow.Domain.Entities;

using Workflow.Domain.Common;
using Workflow.Domain.Enums;

public sealed class WorkflowDefinition
    : AggregateRoot
{
    private readonly List<WorkflowVersion> _versions = [];

    private WorkflowDefinition()
    {
    }

    private WorkflowDefinition(
        Guid id,
        string code,
        string name)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Workflow code is required.",
                nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Workflow name is required.",
                nameof(name));

        Code = code;
        Name = name;
        Status = WorkflowStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; } 

    public bool IsActive { get; private set; } 

    public WorkflowStatus Status { get; private set; }

    public IReadOnlyCollection<WorkflowVersion> Versions
        => _versions.AsReadOnly();

    public static WorkflowDefinition Create(
        string code,
        string name)
    {
        return new WorkflowDefinition(
            Guid.NewGuid(),
            code,
            name);
    }

    public WorkflowVersion CreateVersion(
        int versionNumber)
    {
        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException(
                "Archived workflow cannot create a new version.");

        if (_versions.Any(x =>
            x.VersionNumber == versionNumber))
        {
            throw new InvalidOperationException(
                $"Version {versionNumber} already exists.");
        }

        var version = WorkflowVersion.Create(
            Id,
            versionNumber);

        _versions.Add(version);

        MarkAsModified();

        return version;
    }

    public void Publish()
    {
        if (!_versions.Any())
            throw new InvalidOperationException(
                "Workflow must have at least one version.");

        Status = WorkflowStatus.Published;

        MarkAsModified();
    }

    public void Archive()
    {
        Status = WorkflowStatus.Archived;

        MarkAsModified();
    }
}
