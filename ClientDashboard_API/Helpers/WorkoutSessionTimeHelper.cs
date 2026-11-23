namespace ClientDashboard_API.Helpers
{
    public static class WorkoutSessionTimeHelper
    {
        public const string FutureSessionMessage = "Workouts can only be logged for sessions that have already started";

        private static readonly TimeOnly DefaultStartTime = new(12, 0);

        // The start for a new manual workout: the start sent by the trainer, otherwise 12:00 on the chosen date
        public static DateTime GetManualStartUtc(DateTime? requestedStartUtc, DateOnly sessionDate, TimeZoneInfo trainerZone)
        {
            return requestedStartUtc ?? TimeZoneInfo.ConvertTimeToUtc(sessionDate.ToDateTime(DefaultStartTime), trainerZone);
        }

        // The new start for an edited workout: the start sent by the trainer, otherwise the existing start's
        // clock time moved onto the new date. Workouts logged before session times existed stay without one.
        public static DateTime? GetUpdatedStartUtc(DateTime? requestedStartUtc, DateTime? existingStartUtc, DateOnly newSessionDate, TimeZoneInfo trainerZone)
        {
            if (requestedStartUtc.HasValue)
            {
                return requestedStartUtc.Value;
            }
            if (!existingStartUtc.HasValue)
            {
                return null;
            }

            var existingLocalStart = TimeZoneInfo.ConvertTimeFromUtc(existingStartUtc.Value, trainerZone);
            var movedLocalStart = newSessionDate.ToDateTime(TimeOnly.FromDateTime(existingLocalStart));
            return TimeZoneInfo.ConvertTimeToUtc(movedLocalStart, trainerZone);
        }

        // The trainer-local date of the session: from the start time when there is one, otherwise the date sent
        public static DateOnly GetSessionDate(DateTime? startUtc, string requestedSessionDate, TimeZoneInfo trainerZone)
        {
            return startUtc.HasValue
                ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc.Value, trainerZone))
                : DateOnly.Parse(requestedSessionDate);
        }

        // A future workout would be counted before it happens (and collected again by the nightly sync).
        // With a known start the time is checked; without one only the date can be.
        public static bool IsInFuture(DateTime? startUtc, DateOnly sessionDate, TimeZoneInfo trainerZone, DateTime nowUtc)
        {
            if (startUtc.HasValue)
            {
                return startUtc.Value > nowUtc;
            }
            return sessionDate > DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, trainerZone));
        }
    }
}
