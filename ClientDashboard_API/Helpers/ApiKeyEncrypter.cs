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

        public string Decrypt(string encryptedText)
        {
            var encryptedData = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = key;

            // Extract IV from the beginning
            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract encrypted bytes
            var encryptedBytes = new byte[encryptedData.Length - iv.Length];
            Buffer.BlockCopy(encryptedData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

    }
}
