namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IApiKeyEncryter
    {
        string Encrypt(string plainText);

        string Decrypt(string excryptedText);
    }
}
