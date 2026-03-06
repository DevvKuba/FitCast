using ClientDashboard_API.ML.Enums;

namespace ClientDashboard_API.DTOs
{
    public class PredictionResultDto
    {
        public int TrainerId { get; set; }

        public float PredictedRevenue { get; set; }

        public float? LowerBound { get; set; }

        public float? UpperBound { get; set; }

        public DateTime PredictedDate { get; set; }

        public required string Confidence { get; set; }

        public double RSquared { get; set; }

        public int MonthsOfData { get; set; }

        public required string Message { get; set; }


    }
}
