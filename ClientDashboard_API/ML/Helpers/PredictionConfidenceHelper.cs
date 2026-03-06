using ClientDashboard_API.ML.Enums;

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

        public static (float lowerBound, float upperBound) CalculatePredictionRange(float prediction,double meanAbsoluteError,PredictionConfidence confidence)
        {
            
            // Widen error margin based on confidence
            double errorMultiplier = confidence switch
            {
                PredictionConfidence.Low => 2.5,      // ±250% of MAE
                PredictionConfidence.Medium => 1.5,   // ±150% of MAE
                PredictionConfidence.High => 1.0,     // ±100% of MAE
                PredictionConfidence.VeryHigh => 0.75, // ±75% of MAE
                _ => 2.0
            };

            float margin = (float)(meanAbsoluteError * errorMultiplier);
            // never negative
            return (Math.Max(1, prediction - margin), prediction + margin);
            
        }

        public static string GetConfidenceMessage(PredictionConfidence confidence, double rSquared)
        {
            return confidence switch
            {
                PredictionConfidence.Low =>
                    $"Prediction based on limited data (R²={rSquared:F2}). Confidence will improve as you use the system more.",

                PredictionConfidence.Medium =>
                    $"Moderate confidence prediction (R²={rSquared:F2}). Add more data for better accuracy.",

                PredictionConfidence.High =>
                    $"High confidence prediction (R²={rSquared:F2}).",

                PredictionConfidence.VeryHigh =>
                    $"Very high confidence prediction based on extensive history (R²={rSquared:F2}).",

                _ => "Unable to determine confidence level."
            };
        }
    }
}
