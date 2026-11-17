using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workflow.Application.Abstractions;
using Workflow.Application.Services;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Persistence.Repositories;
using Workflow.Infrastructure.Persistence.Outbox;

namespace Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkflowDbContext>(
            options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString(
                        "WorkflowDatabase"));
            });

        services.AddScoped<IWorkflowUnitOfWork,WorkflowUnitOfWork>();
        services.AddScoped<IWorkflowOutbox, WorkflowOutboxRepository>();
        services.AddScoped<IWorkflowVersionRepository,WorkflowVersionRepository>();
        services.AddScoped<IWorkflowInstanceRepository,WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowDefinitionRepository,WorkflowDefinitionRepository>();

        return services;
    }

    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowEngine, Workflow.Application.Services.WorkflowEngine>();
        services.AddScoped<IWorkflowTransitionResolver,WorkflowTransitionResolver>();
        services.AddScoped<IWorkflowConditionEvaluator,WorkflowConditionEvaluator>();
        services.AddScoped<IWorkflowAssignmentResolver,WorkflowAssignmentResolver>();
        services.AddScoped<IWorkflowTaskAuthorizationService,WorkflowTaskAuthorizationService>();

        return services;
    }
}
