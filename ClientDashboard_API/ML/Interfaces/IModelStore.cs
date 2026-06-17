using Microsoft.ML;

namespace ClientDashboard_API.ML.Interfaces
{
    public interface IModelStore
    {
        Task SaveModelAsync(int trainerId, ITransformer model, DataViewSchema schema);
        Task<ITransformer> LoadModelAsync(int trainerId);
    }
}
