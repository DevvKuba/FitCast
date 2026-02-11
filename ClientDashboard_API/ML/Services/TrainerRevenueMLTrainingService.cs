using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.ML;
using Quartz.Logging;

namespace ClientDashboard_API.ML.Services
{
    public class TrainerRevenueMLTrainingService : IMLModelTrainingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TrainerRevenueMLTrainingService> _logger;
        private readonly MLContext _mlContext;
        private readonly string _modelsPath;

        public TrainerRevenueMLTrainingService(
            IUnitOfWork unitOfWork,
            ILogger<TrainerRevenueMLTrainingService> logger,
            IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
            _modelsPath = Path.Combine(environment.ContentRootPath, "ML", "TrainedModels");

            Directory.CreateDirectory(_modelsPath);
        }

        public Task<ModelMetrics> TrainModelAsync(int trainerId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<int, ModelMetrics>> TrainAllModelsAsync()
        {
            throw new NotImplementedException();
        }

    }
}
