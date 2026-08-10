using ClientDashboard_API.Interfaces.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientDashboard_API.Helpers
{
    public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
    {
        public int GetUserId()
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if(sub is null || !int.TryParse(sub, out var id))
            {
                throw new InvalidOperationException("No authenticated user id on request.");
            }
            return id;

        }
    }
}
