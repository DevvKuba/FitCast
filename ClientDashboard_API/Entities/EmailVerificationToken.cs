namespace ClientDashboard_API.Entities
{
    public sealed class EmailVerificationToken
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime ExpiresOnUtc { get; set; }

        public Trainer? Trainer { get; set; } = null;
    }
}
