using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class RevenuePatternsDto
    {
        public decimal AverageSessionsPrice { get; set; }

        public required List<WeeklyMultiplier> BusiestDays { get; set; }

        public required  List<WeeklyMultiplier> LightDays { get; set; }

        public double EndOfMonthSurge { get; set; }
    }
}
