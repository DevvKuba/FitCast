using Twilio.Jwt.AccessToken;

namespace ClientDashboard_API.Helpers
{
    public abstract class TokenBase
    {
        public int Id { get; set; }

        public required string TokenHash { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime ExpiresOnUtc { get; set; }

        public bool IsConsumed { get; set; } = false;

        public DateTime ConsumedAt { get; set; }

        public bool IsValid()
        {
            if (this == null || IsConsumed || DateTime.UtcNow >= ExpiresOnUtc) return false;

            return true;
        }

        public void Consume()
        {
            IsConsumed = true;
            ConsumedAt = DateTime.UtcNow;
        }
    }
}
