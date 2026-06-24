namespace ClientDashboard_API.DTOs
{
    public class CurrentMonthTrainerAnalyticsDto
    {
        public int BaseClients { get; set; }

        public int MonthlyClientSessions { get; set; }

        public int RevenuePerWorkingDay { get; set; }

        public decimal RevenueGenerated { get; set; }
    }
}
