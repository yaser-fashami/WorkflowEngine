using System.Security.Claims;

namespace Workflow.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(
        this ClaimsPrincipal user)
    {
        var userId =
            user.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "User identifier was not found.");
        }

        if (!Guid.TryParse(
            userId,
            out var result))
        {
            throw new UnauthorizedAccessException(
                "User identifier is invalid.");
        }

        return result;
    }
}
