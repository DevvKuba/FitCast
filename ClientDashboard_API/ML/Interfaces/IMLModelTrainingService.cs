using ClientDashboard_API.ML.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace ClientDashboard_API.ML.Interfaces
{
    public interface IMLModelTrainingService
    {
        // trainers a model for a specific trainer
        Task<ModelMetrics> TrainModelAsync(int trainerId);

        // trainers models for all trainers with sufficient data
        Task<Dictionary<int, ModelMetrics>> TrainAllModelsAsync();
    }
}
