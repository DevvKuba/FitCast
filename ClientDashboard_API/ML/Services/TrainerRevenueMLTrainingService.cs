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
            var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            // 5 Pipele decalration
            var pipeline = _mlContext.Transforms
                // Normalise all numeric features to 0-1 range
                .NormalizeMinMax("ActiveClients")
                .Append(_mlContext.Transforms.NormalizeMinMax("TotalSessionsThisMonth"))
                .Append(_mlContext.Transforms.NormalizeMinMax("AverageSessionPrice"))
                .Append(_mlContext.Transforms.NormalizeMinMax("NewClientsThisMonth"))
                .Append(_mlContext.Transforms.NormalizeMinMax("MonthlyRevenueThusFar"))
                .Append(_mlContext.Transforms.NormalizeMinMax("SessionsPerClient"))
                .Append(_mlContext.Transforms.NormalizeMinMax("GrowthRate"))

                // Concatenate all features into a single "Features" column
                .Append(_mlContext.Transforms.Concatenate("Features",
                "ActiveClients",
                "TotalSessionsThisMonth",
                "AverageSessionPrice",
                "NewClientsThisMonth",
                "MonthlyRevenueThusFar",
                "SessionsPerClient",
                "DayOfMonth",
                "GrowthRate"))

                // Train using FastTree algorithm
                .Append(_mlContext.Regression.Trainers.FastTree(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 20,  // free complexity
                    numberOfTrees: 100,  // number of trees in ensemble,
                    minimumExampleCountPerLeaf: 10)); // prevents overfitting

            // 6 training the model
            _logger.LogInformation("Training model for Trainer {TrainerId}..", trainerId);
            var model = pipeline.Fit(trainTestSplit.TrainSet);

            // 7 evaluate on test set
            var predicitions = model.Transform(trainTestSplit.TestSet);
            var metrics = _mlContext.Regression.Evaluate(predicitions, "Label", "Score");

            // 8 save model to disk
            var modelPath = Path.Combine(_modelsPath, $"trainer_{trainerId}_revenue_model.zip");
            _mlContext.Model.Save(model, dataView.Schema, modelPath);
            _logger.LogInformation("Model saved to {Path}", modelPath);

            // 9 return metrics
            return new ModelMetrics
            {
                TrainerId = trainerId,
                TrainerName = trainer.FirstName,
                RSquared = metrics.RSquared,
                MeanAbsoluteError = metrics.MeanAbsoluteError,
                RootMeanSquaredError = metrics.RootMeanSquaredError,
                TrainingExamplesCount = trainingData.Count,
                TrainedAt = DateTime.UtcNow,
                ModelFilePath = modelPath
            };

        }

        public Task<Dictionary<int, ModelMetrics>> TrainAllModelsAsync()
        {
            throw new NotImplementedException();
        }
        
    }
}
