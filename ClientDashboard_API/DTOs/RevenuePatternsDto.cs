using ClientDashboard_API.Records;
using System.Security.Principal;

namespace ClientDashboard_API.DTOs
{
    public class RevenuePatternsDto
    {
        public decimal SessionsPrice { get; set; }

        public int MonthlyWorkingDays { get; set; }

        public double RevenuePerWorkingDay { get; set; }

        public double RevenuePerWorkingWeek { get; set; }

        public double RevenuePerWorkingMonth { get; set; }
    }
}
