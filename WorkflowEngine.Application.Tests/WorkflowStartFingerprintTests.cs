using Workflow.Application.Commands;
using Workflow.Application.Services;
using Xunit;

namespace WorkflowEngine.Application.Tests;

public sealed class WorkflowStartFingerprintTests
{
    [Fact]
    public void Create_ExcludesActorCorrelationAndIdempotencyMetadata()
    {
        var first = CreateCommand(
            startedBy: Guid.NewGuid(),
            correlationId: "trace-1",
            idempotencyKey: "key-1",
            input: new { EmployeeId = 42, Department = "HR" });
        var retry = first with
        {
            StartedBy = Guid.NewGuid(),
            CorrelationId = "trace-2",
            IdempotencyKey = "key-2"
        };

        Assert.Equal(WorkflowStartFingerprint.Create(first), WorkflowStartFingerprint.Create(retry));
    }

    [Fact]
    public void Create_CanonicalizesObjectPropertyOrder()
    {
        var first = CreateCommand(input: new { A = 1, B = new { X = 2, Y = 3 } });
        var reordered = CreateCommand(input: new { B = new { Y = 3, X = 2 }, A = 1 });

        Assert.Equal(WorkflowStartFingerprint.Create(first), WorkflowStartFingerprint.Create(reordered));
    }

    [Fact]
    public void Create_ChangesWhenSemanticInputChanges()
    {
        var first = CreateCommand(input: new { EmployeeId = 42 });
        var changed = CreateCommand(input: new { EmployeeId = 43 });

        Assert.NotEqual(WorkflowStartFingerprint.Create(first), WorkflowStartFingerprint.Create(changed));
    }

    [Fact]
    public void Create_ChangesWhenDocumentMetadataChanges()
    {
        var baseline = CreateCommand() with
        {
            DocumentId = "document-1",
            DocumentVersion = "version-1",
            DocumentHash = "hash-1"
        };
        var changedHash = baseline with { DocumentHash = "hash-2" };

        Assert.NotEqual(
            WorkflowStartFingerprint.Create(baseline),
            WorkflowStartFingerprint.Create(changedHash));
    }

    [Fact]
    public void Create_ChangesWhenDefinitionCodeOrSchemaVersionChanges()
    {
        var baseline = CreateCommand();
        var differentDefinition = baseline with { WorkflowDefinitionCode = "EMPLOYEE_ONBOARDING_V2" };
        var differentSchema = baseline with { ContractSchemaVersion = 2 };

        Assert.NotEqual(WorkflowStartFingerprint.Create(baseline), WorkflowStartFingerprint.Create(differentDefinition));
        Assert.NotEqual(WorkflowStartFingerprint.Create(baseline), WorkflowStartFingerprint.Create(differentSchema));
    }

    private static StartWorkflowCommand CreateCommand(
        Guid? startedBy = null,
        string correlationId = "trace",
        string idempotencyKey = "key",
        object? input = null) =>
        new()
        {
            WorkflowDefinitionCode = "EMPLOYEE_ONBOARDING",
            StartedBy = startedBy ?? Guid.NewGuid(),
            BusinessType = "EmploymentContract",
            BusinessReference = "contract-123",
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            ContractSchemaVersion = 1,
            Input = input ?? new { EmployeeId = 42 }
        };
}
