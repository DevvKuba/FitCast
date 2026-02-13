using ClientDashboard_API.ML.Interfaces;

namespace ClientDashboard_API.ML.Services
{
    public class TrainerRevenueMLPredictionService : IMLPredictionService
    {
        public Task<Dictionary<int, float>> PredictForAllTrainersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<float> PredictNextMonthRevenueAsync(int trainerId)
        {
            throw new NotImplementedException();
        }
    }
}
