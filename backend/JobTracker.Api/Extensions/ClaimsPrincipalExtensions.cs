using System;
using System.Security.Claims;

namespace JobTracker.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetCurrentUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("User identity claim is missing or invalid.");
            }
            return userId;
        }
    }
}