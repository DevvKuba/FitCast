namespace ClientDashboard_API.ML
{
    public enum PredictionConfidence
    {
        Insufficient, // < 2 months (don't predict yet)
        Low, // 2-3 months
        Medium, // 4-6 months
        High, // 7-12 months
        VeryHigh // 12+ months
    }
}
