using Azure.Storage.Blobs;
using ClientDashboard_API.ML.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.ML;

namespace ClientDashboard_API.ML.Helpers
{
    public class BlobModelStore(BlobServiceClient blobServiceClient) : IModelStore
    {
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

        private readonly MLContext _mlContext = new MLContext(seed: 42);

        public async Task<ITransformer> LoadModelAsync(int trainerId)
        {
            var container = _blobServiceClient.GetBlobContainerClient("ml-models");

            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient($"trainer_{trainerId}_revenue_model.zip");

            if (!await blob.ExistsAsync())
            {
                throw new FileNotFoundException($"No trainer model was retrieved for trainer with id: {trainerId}");
            }

            using var stream = new MemoryStream();
            await blob.DownloadToAsync(stream);
            stream.Position = 0;

            var model = _mlContext.Model.Load(stream, out var schema);

            return model;

        }

        public async Task SaveModelAsync(int trainerId, ITransformer model, DataViewSchema schema)
        {
            var container = _blobServiceClient.GetBlobContainerClient("ml-models");

            await container.CreateIfNotExistsAsync();

            var blobName = $"trainer_{trainerId}_revenue_model.zip";
            var blob = container.GetBlobClient(blobName);

            // Save model to memory stream
            using var stream = new MemoryStream();
            _mlContext.Model.Save(model, schema, stream);

            stream.Position = 0;

            await blob.UploadAsync(stream, overwrite: true);
        }
    }
}
