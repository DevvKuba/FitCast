using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface ITrainerDependencyRemovalHelper
    {
        Task RemoveAllTrainerAssociatedDependenciesAsync(Trainer trainer);
    }
}
