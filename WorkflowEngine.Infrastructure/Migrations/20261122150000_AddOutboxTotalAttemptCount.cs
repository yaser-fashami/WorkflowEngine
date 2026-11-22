using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;

#nullable disable

namespace WorkflowEngine.Infrastructure.Migrations;

[DbContext(typeof(WorkflowDbContext))]
[Migration("20261122150000_AddOutboxTotalAttemptCount")]
public partial class AddOutboxTotalAttemptCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TotalAttemptCount",
            table: "OutboxMessages",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TotalAttemptCount",
            table: "OutboxMessages");
    }
}
