using Microsoft.AspNetCore.Mvc;
using Workflow.Api.Contracts;
using Workflow.Api.Extensions;
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
    /// <param name="request">Workflow definition, business scope, idempotency metadata, and input.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The created workflow instance, or the existing instance for an idempotent replay.</returns>
    /// <response code="200">Workflow instance started or existing instance returned.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="409">The idempotency key was previously used with a different request.</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowInstanceDto>> StartAsync(
        [FromBody] StartWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var userId = User.GetUserId();

        var command = new StartWorkflowCommand
        {
            WorkflowDefinitionCode = request.WorkflowDefinitionCode,
            StartedBy = userId,
            BusinessType = request.BusinessType,
            BusinessReference = request.BusinessReference,
            CorrelationId = request.CorrelationId,
            IdempotencyKey = request.IdempotencyKey,
            ContractSchemaVersion = request.ContractSchemaVersion,
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
    }

    /// <summary>Executes an action on a task belonging to a workflow instance.</summary>
    /// <param name="workflowInstanceId">Identifier of the workflow instance.</param>
    /// <param name="request">Task identifier and action to execute.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated workflow instance.</returns>
    /// <response code="200">The action was executed and the updated workflow instance is returned.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="401">The caller is not authenticated.</response>
    [HttpPost("{workflowInstanceId:guid}/actions")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstanceDto>> ExecuteActionAsync(
        Guid workflowInstanceId,
        [FromBody] ExecuteWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var userId = User.GetUserId();

        var command = new ExecuteWorkflowActionCommand(
            workflowInstanceId,
            request.TaskId,
            request.Action,
            userId);

        var result = await _workflowEngine.ExecuteActionAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Checks whether the WorkflowEngine API is responding.</summary>
    /// <returns>A simple API availability response and the current UTC timestamp.</returns>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "API is running",
            time = DateTime.UtcNow
        });
    }
}
