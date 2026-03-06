using Microsoft.ML.Data;

namespace ClientDashboard_API.ML.Models
{
    public class TrainerRevenueData
    {
        // input features

        [LoadColumn(0)]
        public float ActiveClients { get; set; }

        [LoadColumn(1)]
        public float TotalSessionsThisMonth { get; set; }

        [LoadColumn(2)]
        public float AverageSessionPrice { get; set; }

        [LoadColumn(3)]
        public float NewClientsThisMonth { get; set; }

        [LoadColumn(4)]
        public float MonthlyRevenueThusFar { get; set; }

        // engineered features

        [LoadColumn(5)]
        public float SessionsPerClient { get; set; }

        [LoadColumn(6)]
        public float DaysRemainingInMonth { get; set; }

        [LoadColumn(7)]
        public float GrowthRate { get; set; }

        [LoadColumn(8)]
        public float RevenueVelocity { get; set; }

        // what we're predicting

        [LoadColumn(9)]
        [ColumnName("Label")]
        public float NextMonthRevenue { get; set; }
    }
}
