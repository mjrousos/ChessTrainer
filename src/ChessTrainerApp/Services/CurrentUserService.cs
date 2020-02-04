using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MjrChess.Trainer.Services
{
    public class CurrentUserService
    {
        public ClaimsPrincipal? CurrentUser { get; }

        public string? CurrentUserId => CurrentUser?.GetUserId();

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            CurrentUser = httpContextAccessor?.HttpContext.User;
        }
    }
}
