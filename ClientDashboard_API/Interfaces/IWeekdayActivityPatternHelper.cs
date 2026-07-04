using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Records;

namespace ClientDashboard_API.Interfaces
{
    public interface IWeekdayActivityPatternHelper
    {
        List<WeeklyMultiplier> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allrevenueRecords, int averageClientSessions);

        Weekdays ReturnWeekdayEnumFromString(DayOfWeek weekday);
    }
}
