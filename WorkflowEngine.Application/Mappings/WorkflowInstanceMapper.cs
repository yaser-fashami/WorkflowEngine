using Workflow.Application.DTOs;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Application.Mappers;

public static class WorkflowInstanceMapper
{
    public static WorkflowInstanceDto ToDto(this WorkflowInstance instance, WorkflowVersion? version = null)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new WorkflowInstanceDto
        {
            Id =
                instance.Id,

            WorkflowDefinitionId =
                instance.WorkflowDefinitionId,

            WorkflowVersionId =
                instance.WorkflowVersionId,

            Status =
                instance.Status.ToString(),

            StartedBy =
                instance.StartedBy.Value,

            StartedAt =
                instance.StartedAt,

            CompletedBy =
                instance.CompletedBy.Value,

            CompletedAt =
                instance.CompletedAt,

            Tasks =
                instance.Tasks
                    .Select(task =>
                    {
                        var node =
                            version?
                                .Nodes
                                .FirstOrDefault(
                                    x =>
                                        x.Id ==
                                        task.WorkflowNodeId);

                        return task.ToDto(
                            node);
                    })
                    .ToList()
        };
    }

    public static WorkflowTaskDto ToDto(this WorkflowTask task, WorkflowNode? node = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        var assignments =
            task.Assignments
                .ToList();

        var userAssignments =
            assignments
                .Where(x =>
                    x.Type ==
                    WorkflowTaskAssignmentType.User)
                .ToList();

        var roleAssignments =
            assignments
                .Where(x =>
                    x.Type ==
                    WorkflowTaskAssignmentType.Role)
                .ToList();

        var departmentAssignments =
            assignments
                .Where(x =>
                    x.Type ==
                    WorkflowTaskAssignmentType.Department)
                .ToList();

        var primaryAssignment =
            assignments
                .FirstOrDefault();

        return new WorkflowTaskDto
        {
            Id =
                task.Id,

            WorkflowInstanceId =
                task.WorkflowInstanceId,

            NodeId =
                task.WorkflowNodeId,

            NodeCode =
                node?.Code,

            NodeName =
                node?.Name,

            Type =
                task.TaskType.ToString(),

            Status =
                task.Status.ToString(),

            AssignmentType =
                primaryAssignment?
                    .Type
                    .ToString(),

            AssignedUserId =
                userAssignments
                    .Select(x => x.UserId)
                    .FirstOrDefault(
                        x => x.HasValue),

            AssignedRole =
                roleAssignments
                    .Select(x => x.Role)
                    .FirstOrDefault(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x)),

            AssignedDepartmentId =
                departmentAssignments
                    .Select(x => x.DepartmentId)
                    .FirstOrDefault(
                        x => x.HasValue),

            DueDate =
                task.DueDate,

            CompletedAt =
                task.CompletedAt,

            CompletedBy =
                task.CompletedBy
        };
    }
}