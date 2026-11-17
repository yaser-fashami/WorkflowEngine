using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Workflow.Application.Commands;

namespace Workflow.Application.Services;

/// <summary>
/// Creates a stable fingerprint from the semantic content of a workflow start request.
/// Actor identity, correlation identifiers, and idempotency keys are deliberately excluded.
/// </summary>
internal static class WorkflowStartFingerprint
{
    private const string LegacyAlgorithmVersion = "workflow-start-fingerprint:v1\n";
    private const string DocumentAlgorithmVersion = "workflow-start-fingerprint:v2\n";

    public static string Create(StartWorkflowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var hasDocumentMetadata =
            !string.IsNullOrWhiteSpace(command.DocumentId) &&
            !string.IsNullOrWhiteSpace(command.DocumentVersion) &&
            !string.IsNullOrWhiteSpace(command.DocumentHash);

        var payload = hasDocumentMetadata
            ? JsonSerializer.SerializeToElement(new
            {
                command.WorkflowDefinitionCode,
                command.ContractSchemaVersion,
                command.Input,
                command.DocumentId,
                command.DocumentVersion,
                command.DocumentHash
            })
            : JsonSerializer.SerializeToElement(new
            {
                command.WorkflowDefinitionCode,
                command.ContractSchemaVersion,
                command.Input
            });

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(writer, payload);
        }

        var prefix = Encoding.UTF8.GetBytes(
            hasDocumentMetadata ? DocumentAlgorithmVersion : LegacyAlgorithmVersion);
        var bytes = prefix.Concat(stream.ToArray()).ToArray();
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                WriteCanonical(writer, property.Value);
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray())
            {
                WriteCanonical(writer, item);
            }
            writer.WriteEndArray();
        }
        else
        {
            element.WriteTo(writer);
        }
    }
}
