namespace ClientDashboard_API.DTOs
{
    public class PredictionResultDto
    {
        public int TrainerId { get; set; }

        public float PredictedRevenue { get; set; }

        public DateTime PredictedDate { get; set; }
    }
}
