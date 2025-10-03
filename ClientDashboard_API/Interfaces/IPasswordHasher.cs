namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
    }
}
