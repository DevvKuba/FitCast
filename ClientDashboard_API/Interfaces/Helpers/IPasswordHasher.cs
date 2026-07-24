namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
