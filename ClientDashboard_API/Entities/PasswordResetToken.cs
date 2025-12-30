namespace ClientDashboard_API.Entities
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime ExpiresOnUtc { get; set; }

        public bool IsConsumed { get; set; }

        public UserBase? User { get; set; } = null;
    }
}
