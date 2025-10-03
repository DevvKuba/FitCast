namespace ClientDashboard_API.DTOs
{
    public class LoginTrainerDto
    {
        public record Request(string Email, string Password)
        {
            public async Task<string> Handle(Request request)
            {

            }
        }
    }
}
