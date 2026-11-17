using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;

#nullable disable

namespace WorkflowEngine.Infrastructure.Migrations;

[DbContext(typeof(WorkflowDbContext))]
[Migration("20261117000000_AddWorkflowDocumentMetadata")]
public partial class AddWorkflowDocumentMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DocumentId", table: "WorkflowInstances",
            type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "DocumentVersion", table: "WorkflowInstances",
            type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "DocumentHash", table: "WorkflowInstances",
            type: "nvarchar(256)", maxLength: 256, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DocumentId", table: "WorkflowInstances");
        migrationBuilder.DropColumn(name: "DocumentVersion", table: "WorkflowInstances");
        migrationBuilder.DropColumn(name: "DocumentHash", table: "WorkflowInstances");
    }
}
