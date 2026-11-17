using System.Security.Claims;

namespace Workflow.Api.Extensions;

/// <summary>Helpers for reading identity information from an authenticated principal.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's identifier from the NameIdentifier claim.</summary>
    /// <param name="user">Authenticated principal.</param>
    /// <returns>The user identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">The claim is missing or is not a valid GUID.</exception>
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
