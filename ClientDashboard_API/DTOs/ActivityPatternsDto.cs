using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class ActivityPatternsDto
    {
        public required List<WeeklyActivityPattern> AllWeekdays { get; set; }

        public required List<WeeklyActivityPattern> BusiestDays { get; set; }

        public required List<WeeklyActivityPattern> LightDays { get; set; }
    }
}
