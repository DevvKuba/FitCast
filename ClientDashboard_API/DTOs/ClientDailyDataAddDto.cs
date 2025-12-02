namespace ClientDashboard_API.DTOs
{
    public class ClientDailyDataAddDto
    {
        public DateOnly AsOfDate { get; set; }

        public int SessionsIn7d { get; set; }

        public int SessionsIn28d { get; set; }

        public int DaysSinceLastSession { get; set; }

        public int RemainingSessions { get; set; }

        public int DailySteps { get; set; }

        public double AverageSessionDuration { get; set; }

        public decimal LifeTimeValue { get; set; }

        public bool CurrentlyActive { get; set; }
    }
}
