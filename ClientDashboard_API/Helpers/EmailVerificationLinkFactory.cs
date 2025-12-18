using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Helpers
{
    internal sealed class EmailVerificationLinkFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator) : IEmailVerificationLinkFactory
    {
        public string Create(EmailVerificationToken emailVerificationToken)
        {
            string? verificationLink = linkGenerator.GetUriByName(
                httpContextAccessor.HttpContext!,
                "VerifyEmail",
                new { tokenId = emailVerificationToken.Id });

            return verificationLink ?? throw new Exception("Could not create email verification link");
        }
    }
}
