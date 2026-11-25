namespace ClientDashboard_API.Helpers
{
    public static class WorkoutSessionTimeHelper
    {
        public const string FutureSessionMessage = "Workouts can only be logged for sessions that have already started";

        public static DateOnly GetSessionDate(DateTime startUtc, TimeZoneInfo trainerZone)
        {
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, trainerZone));
        }

        // A future workout would be counted before it happens (and collected again by the nightly sync)
        public static bool IsInFuture(DateTime startUtc, DateTime nowUtc)
        {
            return startUtc > nowUtc;
        }
    }
}
