namespace ClientDashboard_API.ML.Helpers
{
    public static class PredictionConfidenceHelper
    {
        public static PredictionConfidence DetermineConfidenceLevel(int monthsOfData)
        {
            return monthsOfData switch
            {
                < 2 => PredictionConfidence.Insufficient,
                >= 2 and <= 3 => PredictionConfidence.Low,
                >= 4 and <= 6 => PredictionConfidence.Medium,
                >= 7 and <= 12 => PredictionConfidence.High,
                _ => PredictionConfidence.VeryHigh
            };
        }
    }
}
