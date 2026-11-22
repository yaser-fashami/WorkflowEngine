using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using WorkflowEngine.API.Authentication;
using Microsoft.IdentityModel.Tokens;
using Workflow.Infrastructure;
using WorkflowEngine.API.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(DevelopmentBearerAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentBearerAuthenticationHandler>(
            DevelopmentBearerAuthenticationHandler.SchemeName,
            _ => { });
}
else
{
    var authority = builder.Configuration["Authentication:Authority"];
    var audience = builder.Configuration["Authentication:Audience"];

    if (string.IsNullOrWhiteSpace(authority))
        throw new InvalidOperationException("Authentication:Authority configuration is required outside Development.");

    if (string.IsNullOrWhiteSpace(audience))
        throw new InvalidOperationException("Authentication:Audience configuration is required outside Development.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
}

builder.Services.AddControllers();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

static bool HasScope(AuthorizationHandlerContext context, string requiredScope)
{
    var scopeClaim = context.User.FindFirst("scope")?.Value;
    return !string.IsNullOrWhiteSpace(scopeClaim) &&
           scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries)
               .Contains(requiredScope, StringComparer.Ordinal);
}

builder.Services.AddAuthorization(options =>
{
    // No API endpoint is anonymous by default, including health diagnostics.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("WorkflowStart", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("service_id");
        policy.RequireAssertion(context => HasScope(context, "workflow.start"));
    });

    options.AddPolicy("WorkflowExecute", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("service_id", "sinahrm-api");
        policy.RequireAssertion(context => HasScope(context, "workflow.execute"));
    });
});

builder.Services.AddWorkflowApplication();
builder.Services.AddWorkflowInfrastructure(builder.Configuration);
builder.Services.AddHostedService<WorkflowOutboxReaperHostedService>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient("WorkflowCallback", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddHostedService<WorkflowOutboxDispatcherHostedService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
