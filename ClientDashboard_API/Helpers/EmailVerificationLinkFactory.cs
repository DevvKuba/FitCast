using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Helpers
{
    internal sealed class EmailVerificationLinkFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        public string Create(EmailVerificationToken emailVerificationToken)
        {
            string? verificationLink = linkGenerator.GetUriByName(
                httpContextAccessor.HttpContext!,
                "VerifyEmail",
                new { token = emailVerificationToken.Id });

            return verificationLink ?? throw new Exception("Could not create email verification link");
        }
    }
}
