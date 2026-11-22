using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;

#nullable disable

namespace WorkflowEngine.Infrastructure.Migrations;

[DbContext(typeof(WorkflowDbContext))]
[Migration("20261122120000_AddOutboxLeaseMetadata")]
public partial class AddOutboxLeaseMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "LeaseToken",
            table: "OutboxMessages",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastAttemptAtUtc",
            table: "OutboxMessages",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Status_LeaseExpiresAtUtc",
            table: "OutboxMessages",
            columns: new[] { "Status", "LeaseExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_Status_LeaseExpiresAtUtc",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(name: "LeaseToken", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "LastAttemptAtUtc", table: "OutboxMessages");
    }
}
