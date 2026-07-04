using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Records
{
    public record WeeklyActivityPattern(Weekdays day, int totalSessions, double multiplier);

    public record WeeklySessionsCounts(Weekdays day, int totalSessions);
}
