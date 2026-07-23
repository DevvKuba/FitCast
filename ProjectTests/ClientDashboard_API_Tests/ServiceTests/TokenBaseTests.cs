using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // IsValid()/Consume() live once on TokenBase and are inherited, unchanged, by both
    // PasswordResetToken and EmailVerificationToken. Run each case against both concrete
    // types to prove the shared logic behaves identically regardless of which entity uses
    // it - this replaces the near-duplicate ValidateTokenAsync/ConsumeToken tests that used
    // to live separately in each repository test file.
    public class TokenBaseTests
    {
        public static IEnumerable<object[]> BothTokenTypes()
        {
            yield return new object[] { MakePasswordResetToken() };
            yield return new object[] { MakeEmailVerificationToken() };
        }

        private static PasswordResetToken MakePasswordResetToken(
            bool isConsumed = false, DateTime? expiresOnUtc = null, DateTime? consumedAt = null)
        {
            return new PasswordResetToken
            {
                UserId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = expiresOnUtc ?? DateTime.UtcNow.AddHours(24),
                IsConsumed = isConsumed,
                ConsumedAt = consumedAt ?? default
            };
        }

        private static EmailVerificationToken MakeEmailVerificationToken(
            bool isConsumed = false, DateTime? expiresOnUtc = null, DateTime? consumedAt = null)
        {
            return new EmailVerificationToken
            {
                TrainerId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = expiresOnUtc ?? DateTime.UtcNow.AddHours(24),
                IsConsumed = isConsumed,
                ConsumedAt = consumedAt ?? default
            };
        }

        [Theory]
        [MemberData(nameof(BothTokenTypes))]
        public void TestIsValidReturnsTrueForFreshUnexpiredToken(TokenBase token)
        {
            Assert.True(token.IsValid());
        }

        [Fact]
        public void TestIsValidReturnsFalseForExpiredPasswordResetToken()
        {
            var token = MakePasswordResetToken(expiresOnUtc: DateTime.UtcNow.AddHours(-1));

            Assert.False(token.IsValid());
        }

        [Fact]
        public void TestIsValidReturnsFalseForExpiredEmailVerificationToken()
        {
            var token = MakeEmailVerificationToken(expiresOnUtc: DateTime.UtcNow.AddHours(-1));

            Assert.False(token.IsValid());
        }

        [Fact]
        public void TestIsValidReturnsFalseForConsumedPasswordResetToken()
        {
            var token = MakePasswordResetToken(isConsumed: true, consumedAt: DateTime.UtcNow.AddMinutes(-5));

            Assert.False(token.IsValid());
        }

        [Fact]
        public void TestIsValidReturnsFalseForConsumedEmailVerificationToken()
        {
            var token = MakeEmailVerificationToken(isConsumed: true, consumedAt: DateTime.UtcNow.AddMinutes(-5));

            Assert.False(token.IsValid());
        }

        [Theory]
        [MemberData(nameof(BothTokenTypes))]
        public void TestConsumeSetsIsConsumedAndConsumedAt(TokenBase token)
        {
            var beforeConsume = DateTime.UtcNow.AddSeconds(-1);

            token.Consume();

            Assert.True(token.IsConsumed);
            Assert.True(token.ConsumedAt >= beforeConsume);
            Assert.True(token.ConsumedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Theory]
        [MemberData(nameof(BothTokenTypes))]
        public void TestConsumeMakesTokenInvalid(TokenBase token)
        {
            Assert.True(token.IsValid());

            token.Consume();

            Assert.False(token.IsValid());
        }
    }
}
