using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Infrastructure.Persistence;
#nullable disable
namespace WorkflowEngine.Infrastructure.Migrations;
public partial class AddWorkflowStartIdempotency : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.AddColumn<string>("BusinessType","WorkflowInstances",type:"nvarchar(100)",maxLength:100,nullable:true);
  migrationBuilder.AddColumn<string>("BusinessReference","WorkflowInstances",type:"nvarchar(200)",maxLength:200,nullable:true);
  migrationBuilder.AddColumn<string>("CorrelationId","WorkflowInstances",type:"nvarchar(100)",maxLength:100,nullable:true);
  migrationBuilder.AddColumn<string>("IdempotencyKey","WorkflowInstances",type:"nvarchar(200)",maxLength:200,nullable:true);
  migrationBuilder.AddColumn<int>("ContractSchemaVersion","WorkflowInstances",type:"int",nullable:true);
  migrationBuilder.AddColumn<string>("InputJson","WorkflowInstances",type:"nvarchar(max)",nullable:true);
  migrationBuilder.AddColumn<string>("RequestFingerprint","WorkflowInstances",type:"nchar(64)",maxLength:64,nullable:true);
  migrationBuilder.Sql("UPDATE [WorkflowInstances] SET [BusinessType]=N'Legacy',[BusinessReference]=CONVERT(nvarchar(36),[Id]),[CorrelationId]=CONVERT(nvarchar(36),[Id]),[IdempotencyKey]=CONCAT(N'legacy-',CONVERT(nvarchar(36),[Id])),[ContractSchemaVersion]=1,[InputJson]=N'{}',[RequestFingerprint]=REPLICATE(N'0',64) WHERE [BusinessType] IS NULL;");
  migrationBuilder.AlterColumn<string>("BusinessType","WorkflowInstances",type:"nvarchar(100)",maxLength:100,nullable:false,oldClrType:typeof(string),oldType:"nvarchar(100)",oldMaxLength:100,oldNullable:true);
  migrationBuilder.AlterColumn<string>("BusinessReference","WorkflowInstances",type:"nvarchar(200)",maxLength:200,nullable:false,oldClrType:typeof(string),oldType:"nvarchar(200)",oldMaxLength:200,oldNullable:true);
  migrationBuilder.AlterColumn<string>("CorrelationId","WorkflowInstances",type:"nvarchar(100)",maxLength:100,nullable:false,oldClrType:typeof(string),oldType:"nvarchar(100)",oldMaxLength:100,oldNullable:true);
  migrationBuilder.AlterColumn<string>("IdempotencyKey","WorkflowInstances",type:"nvarchar(200)",maxLength:200,nullable:false,oldClrType:typeof(string),oldType:"nvarchar(200)",oldMaxLength:200,oldNullable:true);
  migrationBuilder.AlterColumn<int>("ContractSchemaVersion","WorkflowInstances",type:"int",nullable:false,oldClrType:typeof(int),oldType:"int",oldNullable:true);
  migrationBuilder.AlterColumn<string>("InputJson","WorkflowInstances",type:"nvarchar(max)",nullable:false,oldClrType:typeof(string),oldType:"nvarchar(max)",oldNullable:true);
  migrationBuilder.AlterColumn<string>("RequestFingerprint","WorkflowInstances",type:"nchar(64)",maxLength:64,nullable:false,oldClrType:typeof(string),oldType:"nchar(64)",oldMaxLength:64,oldNullable:true);
  migrationBuilder.CreateIndex("UX_WorkflowInstances_BusinessScope_IdempotencyKey","WorkflowInstances",new[]{"BusinessType","BusinessReference","IdempotencyKey"},unique:true);
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.DropIndex("UX_WorkflowInstances_BusinessScope_IdempotencyKey","WorkflowInstances");
  migrationBuilder.DropColumn("BusinessType","WorkflowInstances"); migrationBuilder.DropColumn("BusinessReference","WorkflowInstances");
  migrationBuilder.DropColumn("CorrelationId","WorkflowInstances"); migrationBuilder.DropColumn("IdempotencyKey","WorkflowInstances");
  migrationBuilder.DropColumn("ContractSchemaVersion","WorkflowInstances"); migrationBuilder.DropColumn("InputJson","WorkflowInstances"); migrationBuilder.DropColumn("RequestFingerprint","WorkflowInstances");
 }
}
