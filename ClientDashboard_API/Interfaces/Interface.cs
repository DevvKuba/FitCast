namespace ClientDashboard_API.Interfaces
{
    public interface IApiKeyEncryter
    {
        string Encrypt(string plainText);

        string Decrypt(string plainText);
    }
}
