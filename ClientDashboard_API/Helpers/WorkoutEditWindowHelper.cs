namespace ClientDashboard_API.Helpers
{
    // Workouts feed the nightly revenue snapshots, which are only rebuilt inside this window,
    // so workouts dated before it are locked to keep closed months' analytics final
    public static class WorkoutEditWindowHelper
    {
        public const int GraceDays = 7;

        public const string LockedWorkoutMessage = "Workouts from closed months can't be added, changed or removed, as that month's analytics are final";

        // The previous month stays open for the first GraceDays of a new month, so late-logged sessions still count
        public static DateOnly GetEarliestEditableDate(DateOnly trainerDate)
        {
            var firstOfMonth = new DateOnly(trainerDate.Year, trainerDate.Month, 1);

            if (trainerDate.Day <= GraceDays)
            {
                return firstOfMonth.AddMonths(-1);
            }
            return firstOfMonth;
        }

        public static bool IsLocked(DateOnly sessionDate, DateOnly trainerToday)
        {
            return sessionDate < GetEarliestEditableDate(trainerToday);
        }
    }
}
