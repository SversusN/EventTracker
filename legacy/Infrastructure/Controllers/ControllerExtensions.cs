using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Presentation.Infrastructure.Controllers;

public static class ControllerExtensions
{
    public static Guid GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User identifier is missing or invalid.");
        }
        return userId;
    }
}
