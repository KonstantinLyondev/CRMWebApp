using System.Security.Claims;

namespace CRMWebApp.Helpers
{
    public static class ImpersonationHelpers
    {
        public static bool IsImpersonating(this ClaimsPrincipal user)
            => user.HasClaim("impersonating", "true");

        public static string? GetImpersonatorId(this ClaimsPrincipal user)
            => user.FindFirst("imp_admin_id")?.Value;
    }
}