using ClientDashboard_API.Helpers;

namespace ClientDashboard_API.Entities
{
    public class EmailVerificationToken : TokenBase
    {
        public int TrainerId { get; set; }

        public Trainer? Trainer { get; set; } = null;
    }
}
