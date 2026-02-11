using Microsoft.ML.Data;

namespace ClientDashboard_API.ML.Models
{
    public class TrainerRevenuePrediction
    {
        [ColumnName("Score")]
        public float PredictedRevenue { get; set; }
    }
}
