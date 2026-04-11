using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class TokenProviderTests
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationInMinutes;

        public TokenProviderTests()
        {
            // Create test configuration
            _secretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789012345678901234567890";
            _issuer = "TestIssuer";
            _audience = "TestAudience";
            _expirationInMinutes = 1440; // 1 day

            var configurationData = new Dictionary<string, string>
            {
                { "Jwt_Secret", _secretKey },
                { "Jwt_Issuer", _issuer },
                { "Jwt_Audience", _audience },
                { "Jwt_ExpirationInMinutes", _expirationInMinutes.ToString() }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            _tokenProvider = new TokenProvider(_configuration);
        }

        [Fact]
        public void TestCreateReturnsNonEmptyToken()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "John",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void TestCreateReturnsValidJwtToken()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Jane",
                Email = "jane@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);

            // Assert
            // JWT tokens have 3 parts separated by dots
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        public void TestCreateTokenContainsUserIdClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 42,
                FirstName = "Alice",
                Email = "alice@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            Assert.NotNull(subClaim);
            Assert.Equal("42", subClaim.Value);
        }

        [Fact]
        public void TestCreateTokenContainsFirstNameClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Bob",
                Email = "bob@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var givenNameClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.GivenName);
            Assert.NotNull(givenNameClaim);
            Assert.Equal("Bob", givenNameClaim.Value);
        }

        [Fact]
        public void TestCreateTokenContainsEmailClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Charlie",
                Email = "charlie@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var emailClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
            Assert.NotNull(emailClaim);
            Assert.Equal("charlie@example.com", emailClaim.Value);
        }

        [Fact]
        public void TestCreateTokenContainsRoleClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "David",
                Email = "david@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var roleClaim = claims.FirstOrDefault(c => c.Type == "role");
            Assert.NotNull(roleClaim);
            Assert.Equal("Trainer", roleClaim.Value);
        }

        [Fact]
        public void TestCreateTokenForClientRole()
        {
            // Arrange
            var user = new Client
            {
                Id = 1,
                FirstName = "Eve",
                Email = "eve@example.com",
                Role = UserRole.Client
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var roleClaim = claims.FirstOrDefault(c => c.Type == "role");
            Assert.NotNull(roleClaim);
            Assert.Equal("Client", roleClaim.Value);
        }

        [Fact]
        public void TestCreateTokenHasCorrectIssuer()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Frank",
                Email = "frank@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            // Assert
            Assert.Equal(_issuer, jsonToken.Issuer);
        }

        [Fact]
        public void TestCreateTokenHasCorrectAudience()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Grace",
                Email = "grace@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            // Assert
            Assert.Contains(_audience, jsonToken.Audiences);
        }

        [Fact]
        public void TestCreateTokenHasExpirationClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Henry",
                Email = "henry@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var beforeCreation = DateTime.UtcNow;
            var token = _tokenProvider.Create(user);
            var afterCreation = DateTime.UtcNow;

            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            // Assert
            Assert.NotNull(jsonToken.ValidTo);
            
            // Expiration should be approximately _expirationInMinutes days from now
            var expectedExpiration = beforeCreation.AddDays(_expirationInMinutes);
            var actualExpiration = jsonToken.ValidTo;

            // Allow 1 minute tolerance for test execution time
            Assert.True(actualExpiration >= expectedExpiration.AddMinutes(-1));
            Assert.True(actualExpiration <= afterCreation.AddDays(_expirationInMinutes).AddMinutes(1));
        }

        [Fact]
        public void TestCreateTokenCanBeValidated()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Iris",
                Email = "iris@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);

            // Assert - Token should be valid
            var validationResult = ValidateToken(token);
            Assert.True(validationResult.IsValid);
        }

        [Fact]
        public void TestCreateGeneratesDifferentTokensForSameUser()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Jack",
                Email = "jack@example.com",
                Role = UserRole.Trainer
            };

            // Act - Create tokens at different times (due to exp and iat claims)
            var token1 = _tokenProvider.Create(user);
            System.Threading.Thread.Sleep(1000); // Wait 1 second
            var token2 = _tokenProvider.Create(user);

            // Assert
            Assert.NotEqual(token1, token2); // Different due to different timestamps
        }

        [Fact]
        public void TestCreateTokenForDifferentUsers()
        {
            // Arrange
            var user1 = new Trainer
            {
                Id = 1,
                FirstName = "Kate",
                Email = "kate@example.com",
                Role = UserRole.Trainer
            };

            var user2 = new Client
            {
                Id = 2,
                FirstName = "Leo",
                Email = "leo@example.com",
                Role = UserRole.Client
            };

            // Act
            var token1 = _tokenProvider.Create(user1);
            var token2 = _tokenProvider.Create(user2);

            // Assert - Tokens should be different
            Assert.NotEqual(token1, token2);

            // Verify claims are different
            var claims1 = GetClaimsFromToken(token1);
            var claims2 = GetClaimsFromToken(token2);

            var id1 = claims1.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            var id2 = claims2.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            Assert.NotEqual(id1, id2);

            var role1 = claims1.First(c => c.Type == "role").Value;
            var role2 = claims2.First(c => c.Type == "role").Value;
            Assert.NotEqual(role1, role2);
        }

        [Fact]
        public void TestCreateTokenContainsAllRequiredClaims()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 123,
                FirstName = "Mike",
                Email = "mike@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert - Verify all 4 custom claims are present
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Sub);
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.GivenName);
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Email);
            Assert.Contains(claims, c => c.Type == "role");

            // Verify standard claims
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Exp);
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Iss);
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Aud);
        }

        [Fact]
        public void TestCreateTokenWithSpecialCharactersInName()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "O'Brien-José",
                Email = "obrien@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var givenNameClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.GivenName);
            Assert.NotNull(givenNameClaim);
            Assert.Equal("O'Brien-José", givenNameClaim.Value);
        }

        [Fact]
        public void TestCreateTokenWithLongUserId()
        {
            // Arrange
            var user = new Trainer
            {
                Id = int.MaxValue, // Very large ID
                FirstName = "Nancy",
                Email = "nancy@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            Assert.NotNull(subClaim);
            Assert.Equal(int.MaxValue.ToString(), subClaim.Value);
        }

        [Fact]
        public void TestCreateTokenIsSignedWithCorrectAlgorithm()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Oscar",
                Email = "oscar@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            // Assert
            Assert.Equal(SecurityAlgorithms.HmacSha256, jsonToken.Alg);
        }

        [Fact]
        public void TestCreateTokenSignatureCanBeVerified()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Peter",
                Email = "peter@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);

            // Assert - Validate signature
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey))
            };

            var handler = new JsonWebTokenHandler();
            var result = handler.ValidateTokenAsync(token, validationParameters).Result;

            Assert.True(result.IsValid);
        }

        [Fact]
        public void TestCreateTokenForMultipleUsersProducesUniqueTokens()
        {
            // Arrange
            var users = new List<Trainer>();
            for (int i = 1; i <= 10; i++)
            {
                users.Add(new Trainer
                {
                    Id = i,
                    FirstName = $"User{i}",
                    Email = $"user{i}@example.com",
                    Role = UserRole.Trainer
                });
            }

            // Act
            var tokens = users.Select(u => _tokenProvider.Create(u)).ToList();

            // Assert - All tokens should be unique
            var uniqueTokens = tokens.Distinct().Count();
            Assert.Equal(10, uniqueTokens);
        }

        [Fact]
        public void TestCreateTokenWithVeryLongEmail()
        {
            // Arrange
            var longEmail = new string('a', 200) + "@example.com";
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Quinn",
                Email = longEmail,
                Role = UserRole.Trainer
            };

            // Act
            var token = _tokenProvider.Create(user);
            var claims = GetClaimsFromToken(token);

            // Assert
            var emailClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
            Assert.NotNull(emailClaim);
            Assert.Equal(longEmail, emailClaim.Value);
        }

        [Fact]
        public void TestCreateTokenHasIssuedAtClaim()
        {
            // Arrange
            var user = new Trainer
            {
                Id = 1,
                FirstName = "Rita",
                Email = "rita@example.com",
                Role = UserRole.Trainer
            };

            // Act
            var beforeCreation = DateTime.UtcNow;
            var token = _tokenProvider.Create(user);
            var afterCreation = DateTime.UtcNow;

            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);

            // Assert
            Assert.NotNull(jsonToken.ValidFrom);
            Assert.True(jsonToken.ValidFrom >= beforeCreation.AddSeconds(-5)); // Allow small tolerance
            Assert.True(jsonToken.ValidFrom <= afterCreation.AddSeconds(5));
        }

        // Helper method to extract claims from a JWT token
        private List<Claim> GetClaimsFromToken(string token)
        {
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);
            return jsonToken.Claims.ToList();
        }

        // Helper method to validate a token
        private TokenValidationResult ValidateToken(string token)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            var handler = new JsonWebTokenHandler();
            return handler.ValidateTokenAsync(token, validationParameters).Result;
        }
    }
}

