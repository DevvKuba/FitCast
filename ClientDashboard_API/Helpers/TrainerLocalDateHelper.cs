namespace ClientDashboard_API.Helpers
{
    public static class TrainerLocalDateHelper
    {
        public static DateOnly GetToday(string trainerTimeZoneId, DateTime nowUtc)
        {
            var trainerZone = TimeZoneInfo.FindSystemTimeZoneById(trainerTimeZoneId);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, trainerZone));
        }

        // The last full day for the trainer - the day the nightly jobs have just finished processing
        public static DateOnly GetYesterday(string trainerTimeZoneId, DateTime nowUtc)
        {
            return GetToday(trainerTimeZoneId, nowUtc).AddDays(-1);
        }
    }
}
