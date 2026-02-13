using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.ML;

namespace ClientDashboard_API.ML.Services
{
    public class TrainerRevenueMLPredictionService : IMLPredictionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TrainerRevenueMLPredictionService> _logger;
        private readonly MLContext _mlContext;
        private readonly string _modelsPath;

        // Cache loaded models in memory (avoid loading from disk every time)
        private readonly Dictionary<int, PredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>> _predictionEngines;

        public TrainerRevenueMLPredictionService(
            IUnitOfWork unitOfWork,
            ILogger<TrainerRevenueMLPredictionService> logger,
            IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mlContext = new MLContext();
            _modelsPath = Path.Combine(environment.ContentRootPath, "ML", "TrainedModels");
            _predictionEngines = new Dictionary<int, PredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>>();
        }

        public Task<float> PredictNextMonthRevenueAsync(int trainerId)
        {
            // 1 get or load prediction engine
            if(!_predictionEngines.ContainsKey(trainerId))
            {
                LoadModelForTrainer(trainerId);
            }
        }

        public Task<Dictionary<int, float>> PredictForAllTrainersAsync()
        {
            throw new NotImplementedException();
        }

        private void LoadModelForTrainer(int trainerId)
        {
            var modelPath = Path.Combine(_modelsPath, $"trainer_{trainerId}_revenue_model.zip");

            if(!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    $"No trained model found for Trainer {trainerId}. " +
                    $"Train the model first using the training service.");
            }

            // load the model from disk
            var model = _mlContext.Model.Load(modelPath, out var modelSchema);

            // create a prediction engine - for single predictions
            var predictionEngine = _mlContext.Model
                .CreatePredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>(model);

            _predictionEngines[trainerId] = predictionEngine;

            _logger.LogInformation("Loaded model for Trainer {TrainerId} from {Path}", trainerId, modelPath);
        }

    }
}
