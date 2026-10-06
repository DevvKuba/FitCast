namespace ClientDashboard_API.Helpers
{
    // IANA time zone IDs a trainer can choose from. Stored as-is on Trainer.TimeZoneId and resolved
    // with TimeZoneInfo.FindSystemTimeZoneById, which applies each zone's DST rules per date.
    public static class SupportedTimeZones
    {
        public const string London = "Europe/London";
        public const string Dublin = "Europe/Dublin";
        public const string Lisbon = "Europe/Lisbon";
        public const string Madrid = "Europe/Madrid";
        public const string Paris = "Europe/Paris";
        public const string Amsterdam = "Europe/Amsterdam";
        public const string Berlin = "Europe/Berlin";
        public const string Rome = "Europe/Rome";
        public const string Warsaw = "Europe/Warsaw";
        public const string Athens = "Europe/Athens";
        public const string Dubai = "Asia/Dubai";
        public const string NewYork = "America/New_York";
        public const string Chicago = "America/Chicago";
        public const string Denver = "America/Denver";
        public const string LosAngeles = "America/Los_Angeles";
        public const string Toronto = "America/Toronto";
        public const string Sydney = "Australia/Sydney";
        public const string Auckland = "Pacific/Auckland";

        public const string Default = London;

        public static readonly IReadOnlyList<string> All =
        [
            London, Dublin, Lisbon, Madrid, Paris, Amsterdam, Berlin, Rome, Warsaw, Athens,
            Dubai, NewYork, Chicago, Denver, LosAngeles, Toronto, Sydney, Auckland
        ];

        public static bool IsSupported(string? timeZoneId) =>
            timeZoneId is not null && All.Contains(timeZoneId);
    }
}
