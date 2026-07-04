using ClientDashboard_API.Data.Migrations;
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
            if (averageClientSessions == 0) return [];

            var activityPatterns = new List<WeeklyActivityPattern>();

            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weeklyPatterns = allRevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => (g.Average(r => (r.SessionsToday)), g.Sum(r => r.SessionsToday))
                );

            foreach (var day in weeklyPatterns)
            {
                // formula: weeklyMultiplier = (weekdayAvg / overallAvg) 
                var multiplierValue = Math.Round(day.Value.Item1 / averageClientSessions, 1);
                var totalSessionCount = day.Value.Item2;

                activityPatterns.Add(new WeeklyActivityPattern(ReturnWeekdayEnumFromString(day.Key), totalSessionCount , multiplierValue));
            }

            return activityPatterns;
        }

        public static List<WeeklySessionsCount> GetNumberOfSessionsForWeekdays(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var weekdaySessionsCounts = allRevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Sum(r => (r.SessionsToday)));

            var sessionPatterns = new List<WeeklySessionsCount>();

            foreach(var day in weekdaySessionsCounts)
            {
                sessionPatterns.Add(new WeeklySessionsCount(ReturnWeekdayEnumFromString(day.Key), day.Value));
            }

            return sessionPatterns;
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

