using ClientDashboard_API.Helpers;
using Microsoft.AspNetCore.WebUtilities;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class TokenGeneratorTests
    {
        [Fact]
        public void TestGenerateTokenReturnsNonEmptyString()
        {
            var token = TokenGenerator.GenerateToken();

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void TestGenerateTokenReturnsUniqueValuesAcrossCalls()
        {
            var tokens = new HashSet<string>();

            for (int i = 0; i < 100; i++)
            {
                tokens.Add(TokenGenerator.GenerateToken());
            }

            Assert.Equal(100, tokens.Count);
        }

        [Fact]
        public void TestGenerateTokenDecodesTo32Bytes()
        {
            // 32 random bytes = 256 bits of entropy
            var token = TokenGenerator.GenerateToken();

            var decoded = WebEncoders.Base64UrlDecode(token);

            Assert.Equal(32, decoded.Length);
        }

        [Fact]
        public void TestGenerateTokenIsUrlSafe()
        {
            // Must not contain characters that require percent-encoding in a query string
            var token = TokenGenerator.GenerateToken();

            Assert.DoesNotContain('+', token);
            Assert.DoesNotContain('/', token);
            Assert.DoesNotContain('=', token);
        }

        [Fact]
        public void TestHashTokenIsDeterministicForSameInput()
        {
            var rawToken = TokenGenerator.GenerateToken();

            var hash1 = TokenGenerator.HashToken(rawToken);
            var hash2 = TokenGenerator.HashToken(rawToken);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void TestHashTokenProducesDifferentOutputForDifferentInput()
        {
            var rawToken1 = TokenGenerator.GenerateToken();
            var rawToken2 = TokenGenerator.GenerateToken();

            var hash1 = TokenGenerator.HashToken(rawToken1);
            var hash2 = TokenGenerator.HashToken(rawToken2);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void TestHashTokenIsCaseSensitive()
        {
            var lower = TokenGenerator.HashToken("abcdef");
            var upper = TokenGenerator.HashToken("ABCDEF");

            Assert.NotEqual(lower, upper);
        }

        [Fact]
        public void TestHashTokenReturnsSha256LengthHexString()
        {
            // SHA-256 = 32 bytes = 64 hex characters
            var hash = TokenGenerator.HashToken(TokenGenerator.GenerateToken());

            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9A-F]+$", hash);
        }

        [Fact]
        public void TestHashTokenDoesNotThrowForEmptyString()
        {
            var hash = TokenGenerator.HashToken(string.Empty);

            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length);
        }

        [Fact]
        public void TestGenerateTokenThenHashTokenRoundTripIsConsistentWithDirectHash()
        {
            var rawToken = TokenGenerator.GenerateToken();

            var hashFromGeneratedToken = TokenGenerator.HashToken(rawToken);
            var hashFromSameRawTokenAgain = TokenGenerator.HashToken(rawToken);

            Assert.Equal(hashFromGeneratedToken, hashFromSameRawTokenAgain);
        }
    }
}
