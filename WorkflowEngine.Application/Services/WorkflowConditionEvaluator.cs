using System.Globalization;
using Workflow.Application.Abstractions;

namespace Workflow.Application.Services;

public sealed class WorkflowConditionEvaluator
    : IWorkflowConditionEvaluator
{
    public Task<bool> EvaluateAsync(
        string expression,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(expression))
        {
            return Task.FromResult(true);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result =
            EvaluateExpression(
                expression,
                context);

        return Task.FromResult(result);
    }

    private static bool EvaluateExpression(
        string expression,
        WorkflowContext context)
    {
        var operators = new[]
        {
            "==",
            "!=",
            ">=",
            "<=",
            ">",
            "<"
        };

        foreach (var op in operators)
        {
            var index =
                expression.IndexOf(
                    op,
                    StringComparison.Ordinal);

            if (index < 0)
            {
                continue;
            }

            var left =
                expression[..index]
                    .Trim();

            var right =
                expression[(index + op.Length)..]
                    .Trim();

            return EvaluateComparison(
                left,
                op,
                right,
                context);
        }

        throw new InvalidOperationException(
            $"Unsupported workflow condition: " +
            $"'{expression}'.");
    }

    private static bool EvaluateComparison(
        string propertyName,
        string operatorName,
        string expectedValue,
        WorkflowContext context)
    {
        var actualValue =
            context.Get(propertyName);

        if (actualValue is null)
        {
            return operatorName switch
            {
                "==" =>
                    IsNullValue(expectedValue),

                "!=" =>
                    !IsNullValue(expectedValue),

                _ => false
            };
        }

        var normalizedExpected =
            RemoveQuotes(expectedValue);

        return Compare(
            actualValue,
            normalizedExpected,
            operatorName);
    }

    private static bool Compare(
        object actualValue,
        string expectedValue,
        string operatorName)
    {
        if (actualValue is string actualString)
        {
            return CompareStrings(
                actualString,
                expectedValue,
                operatorName);
        }

        if (actualValue is bool actualBool)
        {
            if (!bool.TryParse(
                expectedValue,
                out var parsedBool))
            {
                throw new InvalidOperationException(
                    $"Value '{expectedValue}' " +
                    $"is not a valid boolean.");
            }

            return operatorName switch
            {
                "==" =>
                    actualBool == parsedBool,

                "!=" =>
                    actualBool != parsedBool,

                _ =>
                    throw new InvalidOperationException(
                        $"Operator '{operatorName}' " +
                        $"is not supported for boolean.")
            };
        }

        if (actualValue is IComparable comparable)
        {
            return CompareComparable(
                comparable,
                actualValue,
                expectedValue,
                operatorName);
        }

        throw new InvalidOperationException(
            $"Type '{actualValue.GetType().Name}' " +
            $"is not supported in workflow conditions.");
    }

    private static bool CompareStrings(
        string actualValue,
        string expectedValue,
        string operatorName)
    {
        var comparison =
            string.Compare(
                actualValue,
                expectedValue,
                StringComparison.OrdinalIgnoreCase);

        return operatorName switch
        {
            "==" => comparison == 0,

            "!=" => comparison != 0,

            ">" => comparison > 0,

            "<" => comparison < 0,

            ">=" => comparison >= 0,

            "<=" => comparison <= 0,

            _ => throw new InvalidOperationException(
                $"Unsupported operator '{operatorName}'.")
        };
    }

    private static bool CompareComparable(
        IComparable comparable,
        object actualValue,
        string expectedValue,
        string operatorName)
    {
        var targetType =
            actualValue.GetType();

        object? convertedValue;

        try
        {
            convertedValue =
                Convert.ChangeType(
                    expectedValue,
                    targetType,
                    CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Value '{expectedValue}' " +
                $"cannot be converted to " +
                $"'{targetType.Name}'.",
                ex);
        }

        var comparison =
            comparable.CompareTo(
                convertedValue);

        return operatorName switch
        {
            "==" => comparison == 0,

            "!=" => comparison != 0,

            ">" => comparison > 0,

            "<" => comparison < 0,

            ">=" => comparison >= 0,

            "<=" => comparison <= 0,

            _ => throw new InvalidOperationException(
                $"Unsupported operator '{operatorName}'.")
        };
    }

    private static string RemoveQuotes(
        string value)
    {
        if (value.Length >= 2 &&
            (
                value.StartsWith("\"") &&
                value.EndsWith("\"")
                ||
                value.StartsWith("'") &&
                value.EndsWith("'")
            ))
        {
            return value[1..^1];
        }

        return value;
    }

    private static bool IsNullValue(
        string value)
    {
        return string.Equals(
            value,
            "null",
            StringComparison.OrdinalIgnoreCase);
    }
}