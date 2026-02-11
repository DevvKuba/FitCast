namespace ClientDashboard_API.ML.Models
{
    public class ModelMetrics
    {
        public int TrainerId { get; set; }

        public string TrainerName { get; set; } = string.Empty;

        // Regression Metrics
        public double RSquared { get; set; } // 0-1 (0.7+ is classified as good)

        public double MeanAbsoluteError { get; set; } // Average $ prediction error

        public double RootMeanSquaredError { get; set; }

        // Training metadata
        public int TrainingExamplesCount { get; set; }

        public DateTime TrainedAt { get; set; }

        public string ModelFilePath { get; set; } = string.Empty;

        public bool IsGoodQuality => RSquared >= 0.7;
    }
}
