using ClientDashboard_API.Records;
using System.Security.Principal;

namespace ClientDashboard_API.DTOs
{
    public class RevenuePatternsDto
    {
        public decimal SessionsPrice { get; set; }

        public int MonthlyWorkingDays { get; set; }

        public double revenuePerWorkingDay { get; set; }

        public double revenuePerWorkingWeek { get; set; }

        public double revenuePerWorkingMonth { get; set; }
    }
}
