namespace ClientDashboard_API.Helpers
{
    // Quick add is pressed just after a session: it ends now and lasts as long as the trainer's recent sessions
    public static class QuickAddSessionHelper
    {
        public const string SessionTitle = "⚡ Quick Added Session";
        public const int AverageLookbackDays = 30;

        private const int DefaultSessionMinutes = 60;

        // Average rounded to 5 minutes, or the default when the trainer has no recent workouts
        public static int GetSessionMinutes(double? averageWorkoutMinutes)
        {
            if (averageWorkoutMinutes is null)
            {
                return DefaultSessionMinutes;
            }
            return Math.Max(5, (int)Math.Round(averageWorkoutMinutes.Value / 5) * 5);
        }

        // Now, rounded down to the quarter hour
        public static DateTime GetSessionEndUtc(DateTime nowUtc)
        {
            return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute / 15 * 15, 0, DateTimeKind.Utc);
        }
    }
}
