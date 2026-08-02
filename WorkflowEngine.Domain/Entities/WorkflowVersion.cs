using Workflow.Domain.Common;
using Workflow.Domain.Enums;
using Workflow.Domain.Entities;

namespace Workflow.Domain.Entities;

public sealed class WorkflowVersion : BaseEntity
{
    private readonly List<WorkflowNode> _nodes = [];

    private WorkflowVersion()
    {
        // Required by EF Core
    }

    private WorkflowVersion(
        Guid id,
        Guid workflowDefinitionId,
        int versionNumber)
        : base(id)
    {
        if (workflowDefinitionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Workflow definition id is required.",
                nameof(workflowDefinitionId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        WorkflowDefinitionId = workflowDefinitionId;

        VersionNumber = versionNumber;

        Status = WorkflowVersionStatus.Draft;

        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WorkflowDefinitionId
    {
        get;
        private set;
    }

    public int VersionNumber
    {
        get;
        private set;
    }

    public WorkflowVersionStatus Status
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAt
    {
        get;
        private set;
    }

    public DateTimeOffset? PublishedAt
    {
        get;
        private set;
    }

    public DateTimeOffset? ArchivedAt
    {
        get;
        private set;
    }

    public IReadOnlyCollection<WorkflowNode> Nodes
        => _nodes.AsReadOnly();

    public static WorkflowVersion Create(
        Guid workflowDefinitionId,
        int versionNumber)
    {
        return new WorkflowVersion(
            Guid.NewGuid(),
            workflowDefinitionId,
            versionNumber);
    }

    public void AddNode(
        WorkflowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        EnsureDraft();

        if (_nodes.Any(x => x.Id == node.Id))
        {
            throw new InvalidOperationException(
                $"Node '{node.Id}' already exists " +
                $"in workflow version.");
        }

        if (_nodes.Any(x =>
            string.Equals(
                x.Code,
                node.Code,
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A node with code '{node.Code}' " +
                $"already exists in workflow version.");
        }

        _nodes.Add(node);

        MarkAsModified();
    }

    public void RemoveNode(
        Guid nodeId)
    {
        EnsureDraft();

        var node =
            _nodes.SingleOrDefault(
                x => x.Id == nodeId);

        if (node is null)
        {
            throw new InvalidOperationException(
                $"Node '{nodeId}' was not found.");
        }

        _nodes.Remove(node);

        MarkAsModified();
    }

    public void Publish()
    {
        if (Status != WorkflowVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only draft workflow versions " +
                "can be published.");
        }

        ValidateBeforePublish();

        Status =
            WorkflowVersionStatus.Published;

        PublishedAt =
            DateTimeOffset.UtcNow;

        MarkAsModified();
    }

    public void Archive()
    {
        if (Status != WorkflowVersionStatus.Published)
        {
            throw new InvalidOperationException(
                "Only published workflow versions " +
                "can be archived.");
        }

        Status =
            WorkflowVersionStatus.Archived;

        ArchivedAt =
            DateTimeOffset.UtcNow;

        MarkAsModified();
    }

    public bool IsDraft()
    {
        return Status ==
            WorkflowVersionStatus.Draft;
    }

    public bool IsPublished()
    {
        return Status ==
            WorkflowVersionStatus.Published;
    }

    public bool IsArchived()
    {
        return Status ==
            WorkflowVersionStatus.Archived;
    }

    private void EnsureDraft()
    {
        if (Status != WorkflowVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "Workflow version can only be modified " +
                "while it is in draft status.");
        }
    }

    private void ValidateBeforePublish()
    {
        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException(
                "Workflow version must contain " +
                "at least one node.");
        }

        var startNodes =
            _nodes
                .Where(x =>
                    x.Type ==
                    WorkflowNodeType.Start)
                .ToList();

        if (startNodes.Count != 1)
        {
            throw new InvalidOperationException(
                "Workflow version must contain " +
                "exactly one start node.");
        }

        var endNodes =
            _nodes
                .Where(x =>
                    x.Type ==
                    WorkflowNodeType.End)
                .ToList();

        if (endNodes.Count == 0)
        {
            throw new InvalidOperationException(
                "Workflow version must contain " +
                "at least one end node.");
        }

        ValidateNodeCodes();

        ValidateTransitions();
    }

    private void ValidateNodeCodes()
    {
        var duplicatedCodes =
            _nodes
                .GroupBy(
                    x => x.Code,
                    StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicatedCodes.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate node codes found: " +
                string.Join(
                    ", ",
                    duplicatedCodes));
        }
    }

    private void ValidateTransitions()
    {
        var nodeIds =
            _nodes
                .Select(x => x.Id)
                .ToHashSet();

        foreach (var node in _nodes)
        {
            foreach (var transition
                in node.OutgoingTransitions)
            {
                if (!nodeIds.Contains(
                    transition.TargetNodeId))
                {
                    throw new InvalidOperationException(
                        $"Transition '{transition.Id}' " +
                        $"points to a node that does not " +
                        $"exist in this workflow version.");
                }
            }
        }
    }
}