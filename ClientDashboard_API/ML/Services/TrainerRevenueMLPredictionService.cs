using Azure.Storage.Blobs;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.ML;

namespace ClientDashboard_API.ML.Services
{
    public class TrainerRevenueMLPredictionService(
        IUnitOfWork unitOfWork,
        ILogger<TrainerRevenueMLPredictionService> logger,
        IModelStore modelStore
        ) : IMLPredictionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<TrainerRevenueMLPredictionService> _logger = logger;
        private readonly MLContext _mlContext = new MLContext();
        private readonly IModelStore _modelStore = modelStore;

        // Cache loaded models in memory (avoid loading from disk every time)
        private readonly Dictionary<int, PredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>> _predictionEngines = new Dictionary<int, PredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>>();

        public async Task<float> PredictNextMonthRevenueAsync(int trainerId)
        {
            if(_predictionEngines.ContainsKey(trainerId))
            {
                _predictionEngines.Remove(trainerId);
            }

            await LoadLocalModelForTrainer(trainerId);

            var predictionEngine = _predictionEngines[trainerId];

            var lastRecord = await _unitOfWork.TrainerDailyRevenueRepository.GetLatestRevenueRecordForTrainerAsync(trainerId);

            if(lastRecord is null)
            {
                throw new FileNotFoundException($"no daily revenue records found for Trainer: {trainerId}");
            }

            var inputData = FeatureEngineeringHelper.PreparePredictionData(lastRecord);

            var prediction = predictionEngine.Predict(inputData);

            _logger.LogInformation(
                "Predicted next month revenue for Trainer {TrainerId}: ${Revenue:F2}",
                trainerId, Math.Round(prediction.PredictedRevenue, 0));

            return (float)Math.Round(prediction.PredictedRevenue, 0);
        }

        // remove and associated test cases
        public async Task<Dictionary<int, float>> PredictForAllTrainersAsync()
        {
            var trainers = await _unitOfWork.TrainerRepository.GetAllTrainersAsync();
            var predictions = new Dictionary<int, float>();

            foreach(var trainer in trainers)
            {
                try
                {
                    var prediction = await PredictNextMonthRevenueAsync(trainer.Id);
                    predictions[trainer.Id] = prediction;
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to predict for Trainer {TrainerId}", trainer.Id);
                }
            }

            return predictions;
        }

        private async Task LoadLocalModelForTrainer(int trainerId)
        {
            // load the model from disk
            var model = await _modelStore.LoadModelAsync(trainerId);

            // create a prediction engine - for single predictions
            var predictionEngine = _mlContext.Model
                .CreatePredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>(model);

            _predictionEngines[trainerId] = predictionEngine;
        }


        //private async Task LoadBlobStorageModelForTrainer(int trainerId)
        //{
        //    var container = _blobServiceClient.GetBlobContainerClient("ml-models");

        //    var blob = container.GetBlobClient($"trainer_{trainerId}_revenue_model.zip");

        //    if(!await blob.ExistsAsync())
        //    {
        //        throw new FileNotFoundException($"No trainer model was retrieved for trainer with id: {trainerId}");
        //    }

        //    using var stream = new MemoryStream();
        //    await blob.DownloadToAsync(stream);
        //    stream.Position = 0;

        //    var model = _mlContext.Model.Load(stream, out var schema);

        //    var predictionEngine = _mlContext.Model
        //       .CreatePredictionEngine<TrainerRevenueData, TrainerRevenuePrediction>(model);
        //}

    }
}
