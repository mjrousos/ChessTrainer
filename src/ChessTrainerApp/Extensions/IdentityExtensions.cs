using System.Security.Claims;

namespace MjrChess.Trainer
{
    public static class IdentityExtensions
    {
        private const string UserIdClaimType = "sub";

        public static string? GetUserId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirstValue(UserIdClaimType);
        }
    }
}
