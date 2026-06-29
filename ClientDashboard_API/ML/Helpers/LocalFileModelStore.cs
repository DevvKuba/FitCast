using ClientDashboard_API.ML.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.ML;
using Quartz.Logging;

namespace ClientDashboard_API.ML.Helpers
{
    public class LocalFileModelStore(IWebHostEnvironment environment, ILogger<LocalFileModelStore> logger) : IModelStore
    {
        private readonly string _modelsPath = Path.Combine(environment.ContentRootPath, "ML", "TrainedModels");

        private readonly MLContext _mlContext = new MLContext(seed: 42); 

        public async Task<ITransformer> LoadModelAsync(int trainerId)
        {
            var modelPath = Path.Combine(_modelsPath, $"trainer_{trainerId}_revenue_model.zip");

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    $"No trained model found for Trainer {trainerId}. " +
                    $"Train the model first using the training service.");
            }

            var model = _mlContext.Model.Load(modelPath, out var modelSchema);

            logger.LogInformation("Loaded model for Trainer {TrainerId} from {Path}", trainerId, modelPath);

            return model;
        }
        
        public async Task SaveModelAsync(int trainerId, ITransformer model, DataViewSchema schema)
        {
            var modelPath = Path.Combine(_modelsPath, $"trainer_{trainerId}_revenue_model.zip");

            Directory.CreateDirectory(_modelsPath);

            _mlContext.Model.Save(model, schema, modelPath);

            logger.LogInformation("Model saved to {Path}", modelPath);
        }
    }
}
