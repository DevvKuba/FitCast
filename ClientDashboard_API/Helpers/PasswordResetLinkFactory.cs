using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Helpers
{
    public class PasswordResetLinkFactory(IConfiguration configuration) : IPasswordResetLinkFactory
    {
        public string Create(string rawToken)
        {
            var frontendUrl = configuration["FrontendUrl"]
                ?? throw new InvalidOperationException("FrontendUrl is not configured");

            string resetRedirectionLink = $"{frontendUrl}/password-reset?token={rawToken}";

            return resetRedirectionLink;
        }
    }
}
