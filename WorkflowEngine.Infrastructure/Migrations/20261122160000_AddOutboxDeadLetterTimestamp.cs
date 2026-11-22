using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;

#nullable disable

namespace WorkflowEngine.Infrastructure.Migrations;

[DbContext(typeof(WorkflowDbContext))]
[Migration("20261122160000_AddOutboxDeadLetterTimestamp")]
public sealed class AddOutboxDeadLetterTimestamp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeadLetterAtUtc",
            table: "OutboxMessages",
            type: "datetimeoffset",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeadLetterAtUtc",
            table: "OutboxMessages");
    }
}
