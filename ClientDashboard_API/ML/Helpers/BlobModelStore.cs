using Azure.Storage.Blobs;
using ClientDashboard_API.ML.Interfaces;
using Microsoft.ML;

namespace ClientDashboard_API.ML.Helpers
{
    public class BlobModelStore(BlobServiceClient blobServiceClient) : IModelStore
    {
        public Task<ITransformer> LoadModelAsync(int trainerId)
        {
            throw new NotImplementedException();
        }

        public Task SaveModelAsync(int trainerId, ITransformer model, DataViewSchema schema)
        {
            throw new NotImplementedException();
        }
    }
}
