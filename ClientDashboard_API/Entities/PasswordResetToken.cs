using ClientDashboard_API.Helpers;

namespace ClientDashboard_API.Entities
{
    public class PasswordResetToken : TokenBase
    {
        public int UserId { get; set; }

        public UserBase? User { get; set; } = null;
    }
}
