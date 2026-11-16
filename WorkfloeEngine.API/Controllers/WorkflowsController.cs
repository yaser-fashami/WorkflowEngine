using Microsoft.AspNetCore.Mvc;
using Workflow.Api.Contracts;
using Workflow.Api.Extensions;
using Workflow.Application.Abstractions;
using Workflow.Application.DTOs;
using Workflow.Application.Commands;

namespace Workflow.Api.Controllers;

[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowsController(IWorkflowEngine workflowEngine)
    {
        _workflowEngine = workflowEngine;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            Input = request.Input
        };

        var result = await _workflowEngine.StartAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{workflowInstanceId:guid}/actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            userId,
            request.Action,
            userId);

        var result = await _workflowEngine.ExecuteActionAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "API is running",
            time = DateTime.UtcNow
        });
    }
}
