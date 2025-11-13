using ClientDashboard_API.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ClientDashboard_API.Helpers
{
    public class ApiKeyEncrypter : IApiKeyEncryter
    {
        private readonly byte[] key;

        public ApiKeyEncrypter(IConfiguration configuration)
        {
            // gets encryption key from configuration
            var keyString = configuration["ApiKeyEncrypter:Key"]
                ?? throw new InvalidOperationException("API Key encryption key not configured");
            key = Convert.FromBase64String(keyString);
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string plainText)
        {
            throw new NotImplementedException();
        }

    }
}
