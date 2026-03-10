using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class PasswordHasherTests
    {
        private readonly IPasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void TestHashReturnsNonEmptyString()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void TestHashReturnsStringWithHyphenSeparator()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.Contains("-", hash);
            var parts = hash.Split("-");
            Assert.Equal(2, parts.Length); // Should have hash-salt format
        }

        [Fact]
        public void TestHashGeneratesDifferentSaltsForSamePassword()
        {
            // Arrange
            var password = "SamePassword123!";

            // Act
            var hash1 = _passwordHasher.Hash(password);
            var hash2 = _passwordHasher.Hash(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // Different hashes due to random salt
            
            // Verify both are valid hashes
            Assert.Contains("-", hash1);
            Assert.Contains("-", hash2);
        }

        [Fact]
        public void TestHashProducesCorrectLengthHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);
            var parts = hash.Split("-");

            // Assert
            // Hash should be 32 bytes = 64 hex characters
            // Salt should be 16 bytes = 32 hex characters
            Assert.Equal(64, parts[0].Length); // Hash part (32 bytes * 2)
            Assert.Equal(32, parts[1].Length); // Salt part (16 bytes * 2)
        }

        [Fact]
        public void TestHashHandlesShortPasswords()
        {
            // Arrange
            var password = "abc";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains("-", hash);
            var parts = hash.Split("-");
            Assert.Equal(64, parts[0].Length);
            Assert.Equal(32, parts[1].Length);
        }

        [Fact]
        public void TestHashHandlesLongPasswords()
        {
            // Arrange
            var password = new string('a', 1000); // 1000 characters

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains("-", hash);
            var parts = hash.Split("-");
            Assert.Equal(64, parts[0].Length);
            Assert.Equal(32, parts[1].Length);
        }

        [Fact]
        public void TestHashHandlesSpecialCharacters()
        {
            // Arrange
            var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/~`";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains("-", hash);
            Assert.True(_passwordHasher.Verify(password, hash));
        }

        [Fact]
        public void TestHashHandlesUnicodeCharacters()
        {
            // Arrange
            var password = "Pässwörd123!日本語🔐";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains("-", hash);
            Assert.True(_passwordHasher.Verify(password, hash));
        }

        [Fact]
        public void TestHashHandlesEmptyString()
        {
            // Arrange
            var password = "";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains("-", hash);
            Assert.True(_passwordHasher.Verify(password, hash)); // Empty password should still hash and verify
        }

        [Fact]
        public void TestVerifyReturnsTrueForCorrectPassword()
        {
            // Arrange
            var password = "CorrectPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act
            var result = _passwordHasher.Verify(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TestVerifyReturnsFalseForIncorrectPassword()
        {
            // Arrange
            var correctPassword = "CorrectPassword123!";
            var incorrectPassword = "WrongPassword123!";
            var hash = _passwordHasher.Hash(correctPassword);

            // Act
            var result = _passwordHasher.Verify(incorrectPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TestVerifyReturnsFalseForSlightlyDifferentPassword()
        {
            // Arrange
            var password = "Password123!";
            var hash = _passwordHasher.Hash(password);

            // Act - Test case sensitivity and minor differences
            var result1 = _passwordHasher.Verify("password123!", hash); // Different case
            var result2 = _passwordHasher.Verify("Password123", hash);  // Missing !
            var result3 = _passwordHasher.Verify("Password123! ", hash); // Extra space

            // Assert
            Assert.False(result1);
            Assert.False(result2);
            Assert.False(result3);
        }

        [Fact]
        public void TestVerifyReturnsTrueForSamePasswordMultipleTimes()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act - Verify multiple times
            var result1 = _passwordHasher.Verify(password, hash);
            var result2 = _passwordHasher.Verify(password, hash);
            var result3 = _passwordHasher.Verify(password, hash);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
        }

        [Fact]
        public void TestHashAndVerifyWorkTogetherForVariousPasswords()
        {
            // Arrange
            var passwords = new[]
            {
                "Simple123!",
                "VeryLongPasswordWithLotsOfCharacters123!@#$",
                "short",
                "P@$$w0rd",
                "12345678",
                "ñoño123"
            };

            foreach (var password in passwords)
            {
                // Act
                var hash = _passwordHasher.Hash(password);
                var verifyCorrect = _passwordHasher.Verify(password, hash);
                var verifyIncorrect = _passwordHasher.Verify(password + "X", hash);

                // Assert
                Assert.True(verifyCorrect, $"Failed to verify correct password: {password}");
                Assert.False(verifyIncorrect, $"Incorrectly verified wrong password: {password}X");
            }
        }

        [Fact]
        public void TestHashProducesUppercaseHexString()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);
            var parts = hash.Split("-");

            // Assert
            // Convert.ToHexString produces uppercase hex
            Assert.Equal(parts[0], parts[0].ToUpper());
            Assert.Equal(parts[1], parts[1].ToUpper());
            Assert.DoesNotContain("a", parts[0]); // Should be uppercase A-F, not lowercase
            Assert.DoesNotContain("b", parts[0]);
        }

        [Fact]
        public void TestVerifyHandlesValidHashFormat()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Assert hash is in correct format
            Assert.Matches(@"^[A-F0-9]{64}-[A-F0-9]{32}$", hash); // 64 char hash + hyphen + 32 char salt

            // Act
            var result = _passwordHasher.Verify(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TestHashIsDeterministicWithSameSalt()
        {
            // This test verifies that the same password + salt produces the same hash
            // We can't easily test this directly since salt is random, but we can verify
            // that Verify works consistently
            
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act - Verify multiple times with same hash
            var verify1 = _passwordHasher.Verify(password, hash);
            var verify2 = _passwordHasher.Verify(password, hash);
            var verify3 = _passwordHasher.Verify(password, hash);

            // Assert - Should always return true for correct password
            Assert.True(verify1);
            Assert.True(verify2);
            Assert.True(verify3);
        }

        [Fact]
        public void TestHashUsesSecureRandomSaltGeneration()
        {
            // Arrange
            var password = "TestPassword123!";
            var hashes = new List<string>();

            // Act - Generate multiple hashes
            for (int i = 0; i < 100; i++)
            {
                hashes.Add(_passwordHasher.Hash(password));
            }

            // Assert - All hashes should be unique (due to random salts)
            var uniqueHashes = hashes.Distinct().Count();
            Assert.Equal(100, uniqueHashes); // All 100 should be different
        }

        [Fact]
        public void TestVerifyReturnsFalseForManipulatedHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);
            var parts = hash.Split("-");

            // Act - Manipulate the hash slightly
            var manipulatedHash1 = parts[0].Remove(0, 1) + "0" + "-" + parts[1]; // Change first char
            var manipulatedHash2 = parts[0] + "-" + parts[1].Remove(0, 1) + "0"; // Change salt

            var result1 = _passwordHasher.Verify(password, manipulatedHash1);
            var result2 = _passwordHasher.Verify(password, manipulatedHash2);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
        }

        [Fact]
        public void TestHashProducesConsistentLengthForDifferentPasswords()
        {
            // Arrange
            var passwords = new[] { "a", "short", "mediumlength123", new string('x', 500) };

            // Act & Assert
            foreach (var password in passwords)
            {
                var hash = _passwordHasher.Hash(password);
                var parts = hash.Split("-");
                
                // All hashes should have same length regardless of password length
                Assert.Equal(64, parts[0].Length); // Hash
                Assert.Equal(32, parts[1].Length); // Salt
            }
        }

        [Fact]
        public void TestVerifyIsCaseSensitive()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act
            var lowerCase = _passwordHasher.Verify("testpassword123!", hash);
            var upperCase = _passwordHasher.Verify("TESTPASSWORD123!", hash);
            var correct = _passwordHasher.Verify("TestPassword123!", hash);

            // Assert
            Assert.False(lowerCase);
            Assert.False(upperCase);
            Assert.True(correct);
        }

        [Fact]
        public void TestHashHandlesWhitespaceInPasswords()
        {
            // Arrange
            var password1 = "Password With Spaces";
            var password2 = "Password  With  Double  Spaces";
            var password3 = " LeadingSpace";
            var password4 = "TrailingSpace ";

            // Act
            var hash1 = _passwordHasher.Hash(password1);
            var hash2 = _passwordHasher.Hash(password2);
            var hash3 = _passwordHasher.Hash(password3);
            var hash4 = _passwordHasher.Hash(password4);

            // Assert - Verify all hash correctly
            Assert.True(_passwordHasher.Verify(password1, hash1));
            Assert.True(_passwordHasher.Verify(password2, hash2));
            Assert.True(_passwordHasher.Verify(password3, hash3));
            Assert.True(_passwordHasher.Verify(password4, hash4));

            // Verify whitespace differences matter
            Assert.False(_passwordHasher.Verify("PasswordWithSpaces", hash1));
            Assert.False(_passwordHasher.Verify("Password With  Spaces", hash1));
        }

        [Fact]
        public void TestHashOutputIsValidHexString()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);
            var parts = hash.Split("-");

            // Assert - Both parts should be valid hex strings
            Assert.All(parts[0], c => Assert.True(char.IsDigit(c) || (c >= 'A' && c <= 'F')));
            Assert.All(parts[1], c => Assert.True(char.IsDigit(c) || (c >= 'A' && c <= 'F')));
        }

        [Fact]
        public void TestVerifyHandlesCommonPasswordPatterns()
        {
            // Arrange - Test various common password patterns
            var testCases = new Dictionary<string, string>
            {
                { "Password123!", "Password123!" },
                { "p@ssW0rd", "p@ssW0rd" },
                { "12345678", "12345678" },
                { "qwerty!@#", "qwerty!@#" }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var hash = _passwordHasher.Hash(testCase.Key);
                var verifyCorrect = _passwordHasher.Verify(testCase.Value, hash);
                var verifyWrong = _passwordHasher.Verify("WrongPassword", hash);

                // Assert
                Assert.True(verifyCorrect);
                Assert.False(verifyWrong);
            }
        }

        [Fact]
        public void TestHashAndVerifyWorkWithVeryLongPasswords()
        {
            // Arrange
            var password = new string('a', 10000); // Very long password

            // Act
            var hash = _passwordHasher.Hash(password);
            var verifyCorrect = _passwordHasher.Verify(password, hash);
            var verifyWrong = _passwordHasher.Verify(new string('a', 9999), hash);

            // Assert
            Assert.True(verifyCorrect);
            Assert.False(verifyWrong);
        }

        [Fact]
        public void TestHashProducesUnpredictableOutput()
        {
            // Arrange
            var password = "TestPassword";

            // Act - Generate multiple hashes
            var hashes = Enumerable.Range(0, 10)
                .Select(_ => _passwordHasher.Hash(password))
                .ToList();

            // Assert - All should be unique
            var uniqueCount = hashes.Distinct().Count();
            Assert.Equal(10, uniqueCount);
            
            // All should verify with the original password
            Assert.All(hashes, hash => Assert.True(_passwordHasher.Verify(password, hash)));
        }

        [Fact]
        public void TestVerifyReturnsFalseForEmptyPasswordWhenHashIsNotEmpty()
        {
            // Arrange
            var password = "RealPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act
            var result = _passwordHasher.Verify("", hash);

            // Assert
            Assert.False(result); // Empty string should not match non-empty password
        }

        [Fact]
        public void TestHashAndVerifyHandlePasswordsWithOnlyNumbers()
        {
            // Arrange
            var password = "1234567890";

            // Act
            var hash = _passwordHasher.Hash(password);
            var verifyCorrect = _passwordHasher.Verify("1234567890", hash);
            var verifyWrong = _passwordHasher.Verify("0987654321", hash);

            // Assert
            Assert.True(verifyCorrect);
            Assert.False(verifyWrong);
        }

        [Fact]
        public void TestHashAndVerifyHandlePasswordsWithOnlySymbols()
        {
            // Arrange
            var password = "!@#$%^&*()";

            // Act
            var hash = _passwordHasher.Hash(password);
            var verifyCorrect = _passwordHasher.Verify("!@#$%^&*()", hash);
            var verifyWrong = _passwordHasher.Verify("!@#$%^&*", hash);

            // Assert
            Assert.True(verifyCorrect);
            Assert.False(verifyWrong);
        }

        [Fact]
        public void TestHashProducesDifferentOutputForSimilarPasswords()
        {
            // Arrange
            var password1 = "Password123!";
            var password2 = "Password123!!"; // One extra character

            // Act
            var hash1 = _passwordHasher.Hash(password1);
            var hash2 = _passwordHasher.Hash(password2);

            // Assert - Even with random salts, verify they don't cross-verify
            Assert.False(_passwordHasher.Verify(password1, hash2));
            Assert.False(_passwordHasher.Verify(password2, hash1));
            Assert.True(_passwordHasher.Verify(password1, hash1));
            Assert.True(_passwordHasher.Verify(password2, hash2));
        }
    }
}
