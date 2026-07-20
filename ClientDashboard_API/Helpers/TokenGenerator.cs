using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace ClientDashboard_API.Helpers
{
    public static class TokenGenerator
    {
        public static string GenerateToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

            return WebEncoders.Base64UrlEncode(randomBytes);
        }

        public static string HashToken(string rawToken)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

            return Convert.ToHexString(hash);
        }
    }
}
