using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Interfaces
{
    public interface IMLPredictionService
    {
        // Predicts next month's revenue for a trainer based on current month data
        Task<float> PredictNextMonthRevenueAsync(int trainerId);

        // Gets predictions for all trainers
        Task<Dictionary<int, float>> PredictForAllTrainersAsync();
    }
}
