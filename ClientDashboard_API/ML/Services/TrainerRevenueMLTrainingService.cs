using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
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

            // 1 Fetch data from database
            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);
            if(trainer is null)
            {
                throw new ArgumentException($"Trainer {trainerId} not found");
            }

            var dailyRecords = await _unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

            if(dailyRecords.Count < 60) // Need at least 2 months of data
            {
                throw new InvalidOperationException(
                    $"Insufficient data for Trainer {trainerId}. " +
                    $"Have {dailyRecords.Count} records, need at least 60.");
            }

            // 2 Feature engineering
            var trainingData = FeatureEngineeringHelper.PrepareTrainingData(dailyRecords);
            _logger.LogInformation(
                "Prepared {Count} training examples for Trainer {TrainerId}",
                trainingData.Count, trainerId);

            // 3 Load into ML.NET
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // 4 Split into train/test (80/20 split)
            var traintTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);


        }

        public Task<Dictionary<int, ModelMetrics>> TrainAllModelsAsync()
        {
            throw new NotImplementedException();
        }
        
    }
}
