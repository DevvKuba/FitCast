using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace ClientDashboard_API.Helpers
{
    internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
    {
        public string Create(UserBase user)
        {
            string secretKey = configuration["Jwt_Secret"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    [
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(ClaimTypes.Role, user.Role)
                    ]),
                Expires = DateTime.UtcNow.AddDays(configuration.GetValue<int>("Jwt_ExpirationInMinutes")),
                SigningCredentials = credentials,
                Issuer = configuration["Jwt_Issuer"],
                Audience = configuration["Jwt_Audience"],
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptor);

            return token;
        }
    }
}
