using Workflow.Application.Abstractions;
using Workflow.Application.Commands;
using Workflow.Application.DTOs;

using Workflow.Application.Mappers;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Application.Services;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowDefinitionRepository _definitionRepository;

    private readonly IWorkflowInstanceRepository _instanceRepository;

    private readonly IWorkflowAssignmentResolver _assignmentResolver;

    private readonly IWorkflowUnitOfWork _unitOfWork;

    private readonly IWorkflowOutbox _workflowOutbox;

    private readonly IWorkflowTaskAuthorizationService _taskAuthorizationService;

    private readonly IWorkflowVersionRepository _versionRepository;

    private readonly IWorkflowTransitionResolver _transitionResolver;

    public WorkflowEngine(
        IWorkflowDefinitionRepository definitionRepository,

        IWorkflowInstanceRepository instanceRepository,

        IWorkflowAssignmentResolver assignmentResolver,

        IWorkflowUnitOfWork unitOfWork,

        IWorkflowOutbox workflowOutbox,

        IWorkflowTaskAuthorizationService taskAuthorizationService,

        IWorkflowVersionRepository versionRepository,

        IWorkflowTransitionResolver transitionResolver)
    {
        _definitionRepository = definitionRepository;

        _instanceRepository = instanceRepository;

        _assignmentResolver = assignmentResolver;

        _unitOfWork = unitOfWork;

        _workflowOutbox = workflowOutbox;

        _taskAuthorizationService = taskAuthorizationService;

        _versionRepository = versionRepository;

        _transitionResolver = transitionResolver;
    }


    public async Task<WorkflowInstanceDto> StartAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.StartedByService))
        {
            throw new ArgumentException(
                "StartedByService is required.",
                nameof(command.StartedByService));
        }

        if (string.IsNullOrWhiteSpace(command.WorkflowDefinitionCode))
        {
            throw new ArgumentException(
                "WorkflowDefinitionCode is required.",
                nameof(command.WorkflowDefinitionCode));
        }

        if (string.IsNullOrWhiteSpace(command.BusinessType) || command.BusinessType.Length > 100) throw new ArgumentException("Invalid BusinessType.", nameof(command.BusinessType));
        if (string.IsNullOrWhiteSpace(command.BusinessReference) || command.BusinessReference.Length > 200) throw new ArgumentException("Invalid BusinessReference.", nameof(command.BusinessReference));
        if (string.IsNullOrWhiteSpace(command.CorrelationId) || command.CorrelationId.Length > 100) throw new ArgumentException("Invalid CorrelationId.", nameof(command.CorrelationId));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 200) throw new ArgumentException("Invalid IdempotencyKey.", nameof(command.IdempotencyKey));
        if (command.ContractSchemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(command.ContractSchemaVersion));
        var documentMetadataCount = new[] { command.DocumentId, command.DocumentVersion, command.DocumentHash }
            .Count(value => !string.IsNullOrWhiteSpace(value));
        if (documentMetadataCount is not 0 and not 3)
            throw new ArgumentException(
                "DocumentId, DocumentVersion, and DocumentHash must be supplied together.");

        if (command.DocumentId?.Length > 200)
            throw new ArgumentOutOfRangeException(nameof(command.DocumentId));
        if (command.DocumentVersion?.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(command.DocumentVersion));
        if (command.DocumentHash?.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(command.DocumentHash));

        var inputJson = System.Text.Json.JsonSerializer.Serialize(command.Input);
        var fingerprint = WorkflowStartFingerprint.Create(command);
        var existing = await _instanceRepository.GetByIdempotencyKeyAsync(command.BusinessType, command.BusinessReference, command.IdempotencyKey, cancellationToken);
        if (existing is not null) return await EnsureMatchingReplayAsync(existing, fingerprint, cancellationToken);

        // Resolve the stable cross-service code to this service's internal definition.
        var definition =
            await _definitionRepository
                .GetByCodeAsync(
                    command.WorkflowDefinitionCode,
                    cancellationToken);

        if (definition is null)
        {
            throw new InvalidOperationException(
                $"Workflow definition with code '{command.WorkflowDefinitionCode}' was not found.");
        }

        var version =
            await _versionRepository
                .GetPublishedAsync(
                    definition.Id,
                    cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException(
                $"No published workflow version found " +
                $"for workflow definition " +
                $"'{command.WorkflowDefinitionCode}'.");
        }

        var instance = WorkflowInstance.Create(definition.Id, version.Id, command.BusinessType,
            command.BusinessReference, command.CorrelationId, command.IdempotencyKey,
            command.ContractSchemaVersion, inputJson, fingerprint,
            command.DocumentId, command.DocumentVersion, command.DocumentHash);

        instance.Start(command.StartedByService);

        var startNode =
            version.Nodes
                .SingleOrDefault(
                    x => x.Type == WorkflowNodeType.Start);

        if (startNode is null)
        {
            throw new InvalidOperationException(
                $"Workflow version '{version.Id}' " +
                $"does not contain a Start node.");
        }

        var context = BuildContext(instance);

        await ExecuteNodeAsync(
            instance,
            version,
            startNode,
            context,
            completedBy: null,
            cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Completed)
            await EnqueueWorkflowCompletedAsync(instance, cancellationToken);

        await _instanceRepository
            .AddAsync(
                instance,
                cancellationToken);

        try { await _unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (WorkflowInstanceIdempotencyRaceException)
        {
            var winner = await _instanceRepository.GetByIdempotencyKeyAsync(command.BusinessType, command.BusinessReference, command.IdempotencyKey, cancellationToken);
            if (winner is null) throw;
            return await EnsureMatchingReplayAsync(winner, fingerprint, cancellationToken);
        }
        return WorkflowInstanceMapper.ToDto(instance, version);
    }

    private async Task ExecuteNodeAsync(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowNode node,
        WorkflowContext context,
        Guid? completedBy,
        CancellationToken cancellationToken)
    {
        var transition =
                await _transitionResolver.ResolveAsync(
                    node,
                    action: null,
                    context,
                    cancellationToken);

        if (transition is null)
        {
            throw new InvalidOperationException(
                $"No transition found from node " +
                $"'{node.Code}'.");
        }

        switch (node.Type)
        {
            case WorkflowNodeType.Start:

                await MoveToNextNodeAsync(
                    instance,
                    version,
                    transition,
                    context,
                    completedBy,
                    cancellationToken);

                break;

            case WorkflowNodeType.Task:

                await CreateTaskAsync(
                    instance,
                    node,
                    WorkflowTaskType.UserTask,
                    context,
                    cancellationToken);

                break;

            case WorkflowNodeType.Approval:

                await CreateTaskAsync(
                    instance,
                    node,
                    WorkflowTaskType.Approval,
                    context,
                    cancellationToken);

                break;

            case WorkflowNodeType.End:

                if (completedBy.HasValue)
                {
                    instance.Complete(completedBy.Value);
                }
                else
                {
                    instance.CompleteAutomatically();
                }

                break;

            default:

                throw new InvalidOperationException(
                    $"Unsupported node type: {node.Type}");
        }
    }


    public async Task<AuthorizationCheckDto> CheckAuthorizationAsync(
        Guid workflowInstanceId,
        Guid taskId,
        string action,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        var instance = await _instanceRepository.GetByIdWithTasksAsync(
            workflowInstanceId,
            cancellationToken);

        if (instance is null)
            throw new KeyNotFoundException($"Workflow instance {workflowInstanceId} was not found.");

        if (instance.Status != WorkflowInstanceStatus.Running)
            return new AuthorizationCheckDto(false, "TASK_NOT_PENDING", "Workflow instance is not running.");

        var task = instance.Tasks.SingleOrDefault(x => x.Id == taskId);
        if (task is null)
            throw new KeyNotFoundException($"Workflow task {taskId} was not found in workflow instance {workflowInstanceId}.");

        if (task.Status != WorkflowTaskStatus.Pending)
            return new AuthorizationCheckDto(false, "TASK_NOT_PENDING", "Workflow task is not pending.");

        var version = await _versionRepository.GetByIdAsync(
            instance.WorkflowVersionId,
            cancellationToken);

        if (version is null)
            throw new KeyNotFoundException($"Workflow version {instance.WorkflowVersionId} was not found.");

        var currentNode = version.Nodes.SingleOrDefault(x => x.Id == task.WorkflowNodeId);
        if (currentNode is null)
            throw new KeyNotFoundException($"Workflow node {task.WorkflowNodeId} was not found.");

        var isAuthorized = await _taskAuthorizationService.CanExecuteAsync(
            task,
            actorUserId,
            cancellationToken);

        if (!isAuthorized)
            return new AuthorizationCheckDto(false, "NOT_ASSIGNED", "User is not assigned to this task.");

        var context = BuildContext(instance);
        var transition = await _transitionResolver.ResolveAsync(
            currentNode,
            action,
            context,
            cancellationToken);

        return transition is null
            ? new AuthorizationCheckDto(false, "ACTION_NOT_AVAILABLE", "The requested action is not available for this task.")
            : new AuthorizationCheckDto(true, "AUTHORIZED", "User is authorized to execute this action.");
    }

    public async Task<WorkflowInstanceDto> ExecuteActionAsync(
        ExecuteWorkflowActionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PerformedBy == Guid.Empty)
            throw new ArgumentException("PerformedBy is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Action))
            throw new ArgumentException("Action is required.", nameof(command));

        var instance = await _instanceRepository.GetByIdWithTasksAsync(
            command.WorkflowInstanceId,
            cancellationToken);

        if (instance is null)
            throw new KeyNotFoundException($"Workflow instance {command.WorkflowInstanceId} was not found.");

        if (instance.Status != WorkflowInstanceStatus.Running)
            throw new InvalidOperationException("Workflow instance is not running.");

        var task = instance.Tasks.SingleOrDefault(x => x.Id == command.TaskId);
        if (task is null)
            throw new KeyNotFoundException($"Workflow task {command.TaskId} was not found in workflow instance {command.WorkflowInstanceId}.");

        if (task.Status != WorkflowTaskStatus.Pending)
            throw new InvalidOperationException("Workflow task is no longer pending.");

        var version = await _versionRepository.GetByIdAsync(
            instance.WorkflowVersionId,
            cancellationToken);

        if (version is null)
            throw new KeyNotFoundException($"Workflow version {instance.WorkflowVersionId} was not found.");

        var currentNode = version.Nodes.SingleOrDefault(x => x.Id == task.WorkflowNodeId);
        if (currentNode is null)
            throw new KeyNotFoundException($"Workflow node {task.WorkflowNodeId} was not found.");

        var context = BuildContext(instance);

        // Re-check User Assignment at execution time; a prior authorization-check is not reusable.
        var isAuthorized = await _taskAuthorizationService.CanExecuteAsync(
            task,
            command.PerformedBy,
            cancellationToken);

        if (!isAuthorized)
            throw new UnauthorizedAccessException("User is not assigned to this task.");

        // Resolve the requested action before mutating the task or workflow instance.
        var transition = await _transitionResolver.ResolveAsync(
            currentNode,
            command.Action,
            context,
            cancellationToken);

        if (transition is null)
            throw new InvalidOperationException(
                $"No valid transition found for node '{currentNode.Code}' and action '{command.Action}'.");

        var targetNode = version.Nodes.SingleOrDefault(x => x.Id == transition.TargetNodeId);
        if (targetNode is null)
            throw new KeyNotFoundException(
                $"Target node '{transition.TargetNodeId}' was not found in workflow version '{version.Id}'.");

        // First approver wins.
        task.Complete(command.PerformedBy);
        instance.MoveTo(transition.TargetNodeId);

        await ExecuteNodeAsync(
            instance,
            version,
            targetNode,
            context,
            completedBy: command.PerformedBy,
            cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Completed)
            await EnqueueWorkflowCompletedAsync(instance, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return instance.ToDto(version);
    }

    private async Task CreateTaskAsync(
        WorkflowInstance instance,
        WorkflowNode node,
        WorkflowTaskType taskType,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        var task =
            WorkflowTask.Create(
                instance.Id,
                node.Id,
                taskType);

        var assignments =
            await _assignmentResolver
                .ResolveAsync(
                    node,
                    context,
                    cancellationToken);

        if (assignments.Any(assignment => assignment.Type is WorkflowAssignmentType.Role or WorkflowAssignmentType.Department))
            throw new InvalidOperationException("Role and Department assignments are not supported in the current authorization phase.");

        foreach (var assignment in assignments)
        {
            switch (assignment.Type)
            {
                case WorkflowAssignmentType.User:

                    if (assignment.UserId.HasValue)
                    {
                        task.AssignToUser(
                            assignment.UserId.Value);
                    }

                    break;

                case WorkflowAssignmentType.Role:

                    if (!string.IsNullOrWhiteSpace(
                        assignment.Role))
                    {
                        task.AssignToRole(
                            assignment.Role);
                    }

                    break;

                case WorkflowAssignmentType.Department:

                    if (assignment.DepartmentId.HasValue)
                    {
                        task.AssignToDepartment(
                            assignment.DepartmentId.Value);
                    }

                    break;

                default:

                    throw new InvalidOperationException(
                        $"Unsupported assignment type " +
                        $"'{assignment.Type}'.");
            }
        }

        instance.AddTask(task);
    }

    private async Task MoveToNextNodeAsync(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowTransition transition,
        WorkflowContext context,
        Guid? completedBy,
        CancellationToken cancellationToken)
    {
        instance.MoveTo(transition.TargetNodeId);

        var targetNode =
            version.Nodes
                .SingleOrDefault(x =>
                    x.Id == transition.TargetNodeId);

        if (targetNode is null)
        {
            throw new InvalidOperationException(
                $"Target node '{transition.TargetNodeId}' " +
                $"was not found in workflow version " +
                $"'{version.Id}'.");
        }

        await ExecuteNodeAsync(
            instance,
            version,
            targetNode,
            context,
            completedBy,
            cancellationToken);
    }


    private Task EnqueueWorkflowCompletedAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        if (instance.CompletedAt is null)
            throw new InvalidOperationException("A completed workflow must have a completion timestamp.");

        var integrationEvent = new WorkflowCompletedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            BusinessType = instance.BusinessType,
            BusinessReference = instance.BusinessReference,
            CompletedAtUtc = instance.CompletedAt.Value,
            DocumentId = instance.DocumentId,
            DocumentVersion = instance.DocumentVersion,
            DocumentHash = instance.DocumentHash
        };

        return _workflowOutbox.AddAsync(integrationEvent, cancellationToken);
    }

    private static WorkflowContext BuildContext(WorkflowInstance instance)
    {
        var context = new WorkflowContext(instance.Id, instance.WorkflowDefinitionId, instance.WorkflowVersionId);
        using var document = System.Text.Json.JsonDocument.Parse(instance.InputJson);
        if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            foreach (var property in document.RootElement.EnumerateObject()) context.Set(property.Name, property.Value.Clone());
        return context;
    }

    private async Task<WorkflowInstanceDto> EnsureMatchingReplayAsync(WorkflowInstance existing, string fingerprint, CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            throw new WorkflowIdempotencyConflictException("The IdempotencyKey has already been used with a different request payload.");
        var version = await _versionRepository.GetByIdAsync(existing.WorkflowVersionId, cancellationToken);
        if (version is null) throw new InvalidOperationException("Workflow version for existing instance was not found.");
        return WorkflowInstanceMapper.ToDto(existing, version);
    }

    public sealed class WorkflowIdempotencyConflictException(string message) : Exception(message);
    public sealed class WorkflowInstanceIdempotencyRaceException(Exception inner) : Exception("Concurrent idempotency insert.", inner);



}
