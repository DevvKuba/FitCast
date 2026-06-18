using ClientDashboard_API.ML.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.ML;
using Quartz.Logging;

namespace ClientDashboard_API.ML.Helpers
{
    public class LocalFileModelStore(IWebHostEnvironment environment, ILogger logger) : IModelStore
    {
        private string ModelsPath { get; } = Path.Combine(environment.ContentRootPath, "ML", "TrainedModels");

        private MLContext MlContext { get; } = new MLContext(seed: 42);

        public async Task<ITransformer> LoadModelAsync(int trainerId)
        {
            var modelPath = Path.Combine(ModelsPath, $"trainer_{trainerId}_revenue_model.zip");

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    $"No trained model found for Trainer {trainerId}. " +
                    $"Train the model first using the training service.");
            }

            var model = MlContext.Model.Load(modelPath, out var modelSchema);

            logger.LogInformation("Loaded model for Trainer {TrainerId} from {Path}", trainerId, modelPath);

            return model;
        }

        public async Task SaveModelAsync(int trainerId, ITransformer model, DataViewSchema schema)
        {
            var modelPath = Path.Combine(ModelsPath, $"trainer_{trainerId}_revenue_model.zip");

            MlContext.Model.Save(model, schema, ModelsPath);

            logger.LogInformation("Model saved to {Path}", modelPath);
        }
    }
}
