using System.Security.Claims;

namespace MjrChess.Trainer
{
    public static class IdentityExtensions
    {
        private const string UserIdClaimType = ClaimTypes.NameIdentifier;

        public static string? GetUserId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirstValue(UserIdClaimType);
        }
    }
}
