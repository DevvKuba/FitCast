using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Records;

namespace ClientDashboard_API.Helpers
{
    public static class WeekdayActivityPatternHelper
    {
        public static List<WeeklyMultiplier> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allrevenueRecords, int averageClientSessions)
        {
            decimal averageSessionPrice = allrevenueRecords.First().AverageSessionPrice;

            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weekdayAverages = allrevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Average(r => (r.SessionsToday))
                );

            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
            var multipliers = new List<WeeklyMultiplier>();

            foreach (var day in weekdayAverages)
            {
                multipliers.Add(new WeeklyMultiplier(ReturnWeekdayEnumFromString(day.Key), Math.Round(day.Value / averageClientSessions, 1)));
            }

            return multipliers;
        }

        public static Weekdays ReturnWeekdayEnumFromString(DayOfWeek weekday)
        {
            switch (weekday)
            {
                case DayOfWeek.Monday:
                    return Weekdays.Mon;
                case DayOfWeek.Tuesday:
                    return Weekdays.Tue;
                case DayOfWeek.Wednesday:
                    return Weekdays.Wed;
                case DayOfWeek.Thursday:
                    return Weekdays.Thu;
                case DayOfWeek.Friday:
                    return Weekdays.Fri;
                case DayOfWeek.Saturday:
                    return Weekdays.Sat;
                case DayOfWeek.Sunday:
                    return Weekdays.Sun;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weekday), weekday, "Unsupported weekday");
            }
            ;
        }
    }
}
}
