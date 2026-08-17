using System.Security.Claims;

namespace FixMyTownApi.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static int CurrentUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim ?? "0");
        }
    }
}
