using ClientDashboard_API.Records;
using System.Security.Principal;

namespace ClientDashboard_API.DTOs
{
    public class RevenuePatternsDto
    {
        public decimal SessionsPrice { get; set; }

        public int MonthlyWorkingDays { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal RevenuePerWorkingDay { get; set; }

        public decimal RevenuePerWorkingWeek { get; set; }
    }
}
