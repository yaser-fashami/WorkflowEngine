using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;

#nullable disable

namespace WorkflowEngine.Infrastructure.Migrations;

public partial class AddWorkflowStartedByService : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StartedByService",
            table: "WorkflowInstances",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StartedByService",
            table: "WorkflowInstances");
    }
}
