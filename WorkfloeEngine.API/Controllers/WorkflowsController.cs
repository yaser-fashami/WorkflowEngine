using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Api.Contracts;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;
using Workflow.Application.Commands;
using Workflow.Application.Services;

namespace Workflow.Api.Controllers;

/// <summary>Version 1 endpoints for starting and advancing workflow instances.</summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/workflows")]
[Produces("application/json")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowsController(IWorkflowEngine workflowEngine)
    {
        _workflowEngine = workflowEngine;
    }

    /// <summary>Starts a workflow instance for a business object.</summary>
    [Authorize(Policy = "WorkflowStart")]
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowInstanceDto>> StartAsync(
        [FromBody] StartWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var serviceId = User.FindFirst("service_id")?.Value;
        if (string.IsNullOrWhiteSpace(serviceId))
            return Unauthorized();

        var command = new StartWorkflowCommand
        {
            WorkflowDefinitionCode = request.WorkflowDefinitionCode,
            StartedByService = serviceId,
            BusinessType = request.BusinessType,
            BusinessReference = request.BusinessReference,
            CorrelationId = request.CorrelationId,
            IdempotencyKey = request.IdempotencyKey,
            ContractSchemaVersion = request.ContractSchemaVersion,
            DocumentId = request.DocumentId,
            DocumentVersion = request.DocumentVersion,
            DocumentHash = request.DocumentHash,
            Input = request.Input
        };

        try
        {
            return Ok(await _workflowEngine.StartAsync(command, cancellationToken));
        }
        catch (WorkflowEngine.WorkflowIdempotencyConflictException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Idempotency key conflict",
                Detail = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid workflow start request",
                Detail = exception.Message
            });
        }
    }

    /// <summary>Checks whether an actor may execute an action on a task.</summary>
    [Authorize(Policy = "WorkflowExecute")]
    [HttpPost("{workflowInstanceId:guid}/authorization-check")]
    [ProducesResponseType(typeof(AuthorizationCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorizationCheckDto>> CheckAuthorizationAsync(
        Guid workflowInstanceId,
        [FromBody] CheckWorkflowAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Action))
            return BadRequest();

        try
        {
            var result = await _workflowEngine.CheckAuthorizationAsync(
                workflowInstanceId,
                request.TaskId,
                request.Action,
                request.ActorUserId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Workflow resource not found",
                Detail = exception.Message
            });
        }
    }

    /// <summary>Executes an action on a task belonging to a workflow instance.</summary>
    [Authorize(Policy = "WorkflowExecute")]
    [HttpPost("{workflowInstanceId:guid}/actions")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowInstanceDto>> ExecuteActionAsync(
        Guid workflowInstanceId,
        [FromBody] ExecuteWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Action))
            return BadRequest();

        var command = new ExecuteWorkflowActionCommand(
            workflowInstanceId,
            request.TaskId,
            request.Action,
            request.ActorUserId);

        try
        {
            var result = await _workflowEngine.ExecuteActionAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Workflow resource not found",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Workflow action conflict",
                Detail = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid workflow action request",
                Detail = exception.Message
            });
        }
    }

    /// <summary>Checks whether the WorkflowEngine API is responding.</summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    public ActionResult<PingResponse> Ping()
    {
        return Ok(new PingResponse
        {
            Message = "API is running",
            Time = DateTime.UtcNow
        });
    }
}
