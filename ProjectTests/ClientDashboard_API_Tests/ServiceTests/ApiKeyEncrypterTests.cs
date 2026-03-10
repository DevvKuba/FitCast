using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class ApiKeyEncrypterTests
    {
        private readonly IApiKeyEncryter _encrypter;

        public ApiKeyEncrypterTests()
        {
            // Create a fake configuration with a valid 256-bit key (32 bytes = 44 base64 characters)
            var key = Convert.ToBase64String(new byte[32]); // 32 bytes for AES-256
            
            var configurationData = new Dictionary<string, string>
            {
                { "ApiKeyEncrypter:Key", key }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            _encrypter = new ApiKeyEncrypter(configuration);
        }

        [Fact]
        public void TestEncryptReturnsNonEmptyString()
        {
            // Arrange
            var apiKey = "test-api-key-12345";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
        }

        [Fact]
        public void TestEncryptReturnsBase64String()
        {
            // Arrange
            var apiKey = "test-api-key-12345";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);

            // Assert
            // Should be valid base64 (can be decoded without exception)
            var bytes = Convert.FromBase64String(encrypted);
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public void TestEncryptGeneratesDifferentOutputForSameInput()
        {
            // Arrange
            var apiKey = "same-api-key";

            // Act
            var encrypted1 = _encrypter.Encrypt(apiKey);
            var encrypted2 = _encrypter.Encrypt(apiKey);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2); // Different due to random IV
        }

        [Fact]
        public void TestDecryptReturnsOriginalPlainText()
        {
            // Arrange
            var originalApiKey = "my-secret-api-key-12345";

            // Act
            var encrypted = _encrypter.Encrypt(originalApiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(originalApiKey, decrypted);
        }

        [Fact]
        public void TestEncryptAndDecryptWorkTogetherForVariousApiKeys()
        {
            // Arrange
            var apiKeys = new[]
            {
                "short",
                "a-longer-api-key-with-dashes",
                "VeryLongApiKeyWith123NumbersAnd!@#SpecialCharacters",
                "key_with_underscores_123",
                "api-key-with-guid-a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            };

            foreach (var apiKey in apiKeys)
            {
                // Act
                var encrypted = _encrypter.Encrypt(apiKey);
                var decrypted = _encrypter.Decrypt(encrypted);

                // Assert
                Assert.Equal(apiKey, decrypted);
            }
        }

        [Fact]
        public void TestEncryptHandlesEmptyString()
        {
            // Arrange
            var apiKey = "";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
            Assert.NotEmpty(encrypted); // Encrypted version should not be empty (includes IV)
        }

        [Fact]
        public void TestEncryptHandlesSpecialCharacters()
        {
            // Arrange
            var apiKey = "api-key!@#$%^&*()_+-=[]{}|;:',.<>?/~`";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptHandlesUnicodeCharacters()
        {
            // Arrange
            var apiKey = "api-key-日本語-🔐-ñoño";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptHandlesWhitespace()
        {
            // Arrange
            var apiKey1 = "api key with spaces";
            var apiKey2 = " leading space";
            var apiKey3 = "trailing space ";
            var apiKey4 = "  multiple  spaces  ";

            // Act & Assert
            Assert.Equal(apiKey1, _encrypter.Decrypt(_encrypter.Encrypt(apiKey1)));
            Assert.Equal(apiKey2, _encrypter.Decrypt(_encrypter.Encrypt(apiKey2)));
            Assert.Equal(apiKey3, _encrypter.Decrypt(_encrypter.Encrypt(apiKey3)));
            Assert.Equal(apiKey4, _encrypter.Decrypt(_encrypter.Encrypt(apiKey4)));
        }

        [Fact]
        public void TestEncryptHandlesVeryLongApiKeys()
        {
            // Arrange
            var apiKey = new string('x', 10000); // Very long API key

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptProducesDifferentCipherTextForSameInputDueToRandomIV()
        {
            // Arrange
            var apiKey = "consistent-api-key";
            var encryptedKeys = new List<string>();

            // Act - Encrypt the same key multiple times
            for (int i = 0; i < 10; i++)
            {
                encryptedKeys.Add(_encrypter.Encrypt(apiKey));
            }

            // Assert - All encrypted versions should be unique (random IV)
            var uniqueCount = encryptedKeys.Distinct().Count();
            Assert.Equal(10, uniqueCount);

            // But all should decrypt to the same original value
            Assert.All(encryptedKeys, encrypted => 
                Assert.Equal(apiKey, _encrypter.Decrypt(encrypted))
            );
        }

        [Fact]
        public void TestDecryptHandlesDifferentEncryptedVersionsOfSameKey()
        {
            // Arrange
            var apiKey = "test-api-key";
            var encrypted1 = _encrypter.Encrypt(apiKey);
            var encrypted2 = _encrypter.Encrypt(apiKey);

            // Act
            var decrypted1 = _encrypter.Decrypt(encrypted1);
            var decrypted2 = _encrypter.Decrypt(encrypted2);

            // Assert
            Assert.Equal(apiKey, decrypted1);
            Assert.Equal(apiKey, decrypted2);
            Assert.NotEqual(encrypted1, encrypted2); // Different encrypted values
        }

        [Fact]
        public void TestEncryptOutputLengthIsConsistentForSameInputLength()
        {
            // Arrange
            var apiKey1 = "test1";
            var apiKey2 = "test2";
            var apiKey3 = "test3";

            // Act
            var encrypted1 = _encrypter.Encrypt(apiKey1);
            var encrypted2 = _encrypter.Encrypt(apiKey2);
            var encrypted3 = _encrypter.Encrypt(apiKey3);

            // Assert - Same input length should produce similar output length (due to block cipher padding)
            var bytes1 = Convert.FromBase64String(encrypted1);
            var bytes2 = Convert.FromBase64String(encrypted2);
            var bytes3 = Convert.FromBase64String(encrypted3);

            Assert.Equal(bytes1.Length, bytes2.Length);
            Assert.Equal(bytes2.Length, bytes3.Length);
        }

        [Fact]
        public void TestEncryptAndDecryptAreInverseOperations()
        {
            // Arrange
            var apiKeys = new[]
            {
                "api-key-1",
                "ApiKey123!@#",
                "very_long_api_key_with_lots_of_characters_123456",
                "🔐secure🔐key🔐",
                ""
            };

            foreach (var apiKey in apiKeys)
            {
                // Act
                var encrypted = _encrypter.Encrypt(apiKey);
                var decrypted = _encrypter.Decrypt(encrypted);

                // Assert
                Assert.Equal(apiKey, decrypted);
            }
        }

        [Fact]
        public void TestEncryptHandlesRepeatingCharacters()
        {
            // Arrange
            var apiKey = "aaaaaaaaaaaaaaaa"; // Repeating characters

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptHandlesNewlinesAndTabs()
        {
            // Arrange
            var apiKey = "api-key\nwith\nnewlines\tand\ttabs";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptProducesBase64OutputWithoutInvalidCharacters()
        {
            // Arrange
            var apiKey = "test-api-key-12345";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);

            // Assert - Base64 should only contain A-Z, a-z, 0-9, +, /, and =
            Assert.Matches(@"^[A-Za-z0-9+/=]+$", encrypted);
        }

        [Fact]
        public void TestDecryptRevertsMultipleEncryptions()
        {
            // Arrange
            var originalKey = "original-api-key";

            // Act - Encrypt once
            var encrypted1 = _encrypter.Encrypt(originalKey);
            var decrypted1 = _encrypter.Decrypt(encrypted1);

            // Encrypt again (different IV)
            var encrypted2 = _encrypter.Encrypt(originalKey);
            var decrypted2 = _encrypter.Decrypt(encrypted2);

            // Assert - Both decrypt to original
            Assert.Equal(originalKey, decrypted1);
            Assert.Equal(originalKey, decrypted2);
            Assert.NotEqual(encrypted1, encrypted2); // Different ciphertext
        }

        [Fact]
        public void TestEncryptAndDecryptHandleGuidsAsApiKeys()
        {
            // Arrange - Common API key format
            var apiKey = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptAndDecryptHandleLongBase64ApiKeys()
        {
            // Arrange - Some APIs use base64 encoded keys
            var apiKey = Convert.ToBase64String(new byte[64]); // 64 bytes = 88 base64 chars

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptPreservesExactStringLength()
        {
            // Arrange
            var apiKey = "test";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey.Length, decrypted.Length);
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptHandlesNumericApiKeys()
        {
            // Arrange
            var apiKey = "123456789012345678901234567890";

            // Act
            var encrypted = _encrypter.Encrypt(apiKey);
            var decrypted = _encrypter.Decrypt(encrypted);

            // Assert
            Assert.Equal(apiKey, decrypted);
        }

        [Fact]
        public void TestEncryptProducesNonDeterministicOutput()
        {
            // Arrange
            var apiKey = "deterministic-test-key";
            var encryptedValues = new HashSet<string>();

            // Act - Encrypt 50 times
            for (int i = 0; i < 50; i++)
            {
                encryptedValues.Add(_encrypter.Encrypt(apiKey));
            }

            // Assert - All should be unique (due to random IV)
            Assert.Equal(50, encryptedValues.Count);

            // But all should decrypt to the same value
            foreach (var encrypted in encryptedValues)
            {
                Assert.Equal(apiKey, _encrypter.Decrypt(encrypted));
            }
        }

        [Fact]
        public void TestDecryptConsistentlyReturnsOriginalValue()
        {
            // Arrange
            var apiKey = "consistent-api-key";
            var encrypted = _encrypter.Encrypt(apiKey);

            // Act - Decrypt multiple times
            var decrypted1 = _encrypter.Decrypt(encrypted);
            var decrypted2 = _encrypter.Decrypt(encrypted);
            var decrypted3 = _encrypter.Decrypt(encrypted);

            // Assert - Should always return the same original value
            Assert.Equal(apiKey, decrypted1);
            Assert.Equal(apiKey, decrypted2);
            Assert.Equal(apiKey, decrypted3);
        }

        [Fact]
        public void TestEncryptAndDecryptHandleComplexRealWorldApiKeys()
        {
            // Arrange - Real-world API key formats
            var apiKeys = new[]
            {
                "sk_test_51AbCdEfGhIjKlMnOpQrStUvWxYz",
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
                "AIzaSyD-9tNQ8vK4pZ_example_key_123456",
                "pk_live_1234567890abcdefghijklmnop"
            };

            foreach (var apiKey in apiKeys)
            {
                // Act
                var encrypted = _encrypter.Encrypt(apiKey);
                var decrypted = _encrypter.Decrypt(encrypted);

                // Assert
                Assert.Equal(apiKey, decrypted);
            }
        }
    }
}
