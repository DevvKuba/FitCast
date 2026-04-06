using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class ActivityPatternsDto
    {
        public required List<WeeklyMultiplier> AllWeekdays { get; set; }

        public required List<WeeklyMultiplier> BusiestDays { get; set; }

        public required List<WeeklyMultiplier> LightDays { get; set; }
    }
}
