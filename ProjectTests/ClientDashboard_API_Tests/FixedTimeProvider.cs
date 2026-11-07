namespace ClientDashboard_API_Tests
{
    // Pins "now" so time-of-day dependent logic can be asserted deterministically.
    public class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
