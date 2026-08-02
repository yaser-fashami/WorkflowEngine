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

    private readonly IWorkflowTaskAuthorizationService _taskAuthorizationService;

    private readonly IWorkflowVersionRepository _versionRepository;

    private readonly IWorkflowTransitionResolver _transitionResolver;

    public WorkflowEngine(
        IWorkflowDefinitionRepository definitionRepository,

        IWorkflowInstanceRepository instanceRepository,

        IWorkflowAssignmentResolver assignmentResolver,

        IWorkflowUnitOfWork unitOfWork,

        IWorkflowTaskAuthorizationService taskAuthorizationService,

        IWorkflowVersionRepository versionRepository,

        IWorkflowTransitionResolver transitionResolver)
    {
        _definitionRepository = definitionRepository;

        _instanceRepository = instanceRepository;

        _assignmentResolver = assignmentResolver;

        _unitOfWork = unitOfWork;

        _taskAuthorizationService = taskAuthorizationService;

        _versionRepository = versionRepository;

        _transitionResolver = transitionResolver;
    }


    public async Task<WorkflowInstanceDto> StartAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.StartedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "StartedBy is required.",
                nameof(command.StartedBy));
        }

        var version =
            await _versionRepository
                .GetPublishedAsync(
                    command.WorkflowDefinitionId,
                    cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException(
                $"No published workflow version found " +
                $"for workflow definition " +
                $"'{command.WorkflowDefinitionId}'.");
        }

        var instance =
            WorkflowInstance.Create(
                version.WorkflowDefinitionId,
                version.Id);

        instance.Start(
            command.StartedBy);

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

        var context =
            BuildContext(
                instance,
                command.StartedBy);

        await ExecuteNodeAsync(
            instance,
            version,
            startNode,
            context,
            cancellationToken);

        await _instanceRepository
            .AddAsync(
                instance,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return WorkflowInstanceMapper.ToDto(instance, version);
    }

    private async Task ExecuteNodeAsync(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowNode node,
        WorkflowContext context,
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

                instance.Complete(
                    context.UserId);

                break;

            default:

                throw new InvalidOperationException(
                    $"Unsupported node type: {node.Type}");
        }
    }


    public async Task<WorkflowInstanceDto> ExecuteActionAsync(ExecuteWorkflowActionCommand command, CancellationToken cancellationToken)
    {
        var instance =
            await _instanceRepository
                .GetByIdWithTasksAsync(
                    command.WorkflowInstanceId,
                    cancellationToken);

        if (instance is null)
        {
            throw new InvalidOperationException(
                $"Workflow instance {command.WorkflowInstanceId} not found.");
        }

        var task = instance.Tasks.SingleOrDefault(x => x.Id == command.TaskId);

        if (task is null)
        {
            throw new InvalidOperationException(
                "Workflow task not found.");
        }

        if (task.Status != WorkflowTaskStatus.Pending)
        {
            throw new InvalidOperationException(
                "Workflow task is no longer pending.");
        }

        var isAuthorized = await _taskAuthorizationService
                .CanExecuteAsync(
                    task,
                    command.PerformedBy,
                    cancellationToken);

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException(
                "User is not authorized to execute this task.");
        }

        var version = await _versionRepository
                .GetByIdAsync(
                    instance.WorkflowVersionId,
                    cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException(
                "Workflow version not found.");
        }

        var currentNode = version.Nodes
                .SingleOrDefault(x =>
                    x.Id == task.WorkflowNodeId);

        if (currentNode is null)
        {
            throw new InvalidOperationException(
                "Current workflow node not found.");
        }

        var context = BuildContext(
                instance,
                command.PerformedBy);

        // First approver wins.
        task.Complete(command.PerformedBy);

        var transition =
            await _transitionResolver
                .ResolveAsync(
                    currentNode,
                    command.Action,
                    context,
                    cancellationToken);

        if (transition is null)
        {
            throw new InvalidOperationException(
                $"No valid transition found " +
                $"for node '{currentNode.Code}' " +
                $"and action '{command.Action}'.");
        }

        instance.MoveTo(transition.TargetNodeId);

        var targetNode =
            version.Nodes
                .SingleOrDefault(x =>
                    x.Id == transition.TargetNodeId);

        if (targetNode is null)
        {
            throw new InvalidOperationException(
                $"Target node '{transition.TargetNodeId}' " +
                $"was not found.");
        }

        await ExecuteNodeAsync(
            instance,
            version,
            targetNode,
            context,
            cancellationToken);

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
            cancellationToken);
    }


    private static WorkflowContext BuildContext(
    WorkflowInstance instance,
    Guid userId)
    {
        return new WorkflowContext(
            workflowInstanceId: instance.Id,
            workflowDefinitionId:
                instance.WorkflowDefinitionId,
            workflowVersionId:
                instance.WorkflowVersionId,
            userId: userId);
    }

}
