using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Records;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.Helpers
{
    public static class WeekdayActivityPatternHelper
    {
        public static List<WeeklyActivityPattern> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allRevenueRecords, int averageClientSessions)
        {
            decimal averageSessionPrice = allRevenueRecords.First().AverageSessionPrice;

            var activityPatterns = FillNumberOfSessionsForWeekdays(allRevenueRecords);

            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weekdayAverages = allRevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Average(r => (r.SessionsToday))
                );

            foreach (var day in weekdayAverages)
            {
                // formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
                activityPatterns.Add(new WeeklyActivityPattern(ReturnWeekdayEnumFromString(day.Key), 0, Math.Round(day.Value / averageClientSessions, 1)));
            }

            return activityPatterns;
        }

        public static List<WeeklyActivityPattern> FillNumberOfSessionsForWeekdays(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var weekdaySessionsCounts = allRevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Sum(r => (r.SessionsToday)));

            var activityPatterns = new List<WeeklyActivityPattern>();

            foreach(var day in weekdaySessionsCounts)
            {
                activityPatterns.Add(new WeeklyActivityPattern(ReturnWeekdayEnumFromString(day.Key), day.Value, 0));
            }

            return activityPatterns;
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
