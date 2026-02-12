using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.ML;
using Quartz.Logging;
using System.CodeDom;
using Twilio.Rest.Api.V2010.Account.Usage.Record;

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

        public async Task<ModelMetrics> TrainModelAsync(int trainerId)
        {
            _logger.LogInformation("Starting model training for Trainer ID: {TrainerId}", trainerId);

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);
            if(trainer is null)
            {
                throw new ArgumentException($"Trainer {trainerId} not found");
            }

            var dailyRecords = await _unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);
        }

        public Task<Dictionary<int, ModelMetrics>> TrainAllModelsAsync()
        {
            throw new NotImplementedException();
        }
        
    }
}
