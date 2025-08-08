using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    // look into seperating logic differently, inject more into respective controller
    public class SessionSyncService(IUnitOfWork unitOfWork, ISessionDataParser hevyParser) : ISessionSyncService
    {
        // consider having all the HevyApi logic in a seperate project in itself
        public async Task<bool> SyncDailySessions()
        {
            // gathers all the data 
            var dailyWorkouts = await hevyParser.CallApi();

            if (dailyWorkouts == null) return false;

            foreach (var workout in dailyWorkouts)
            {
                string clientName = workout.Title.Split(' ')[0];
                if (await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName))
                {
                    await unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(clientName);

                }
                else
                {
                    await unitOfWork.ClientRepository.AddNewClientAsync(clientName);
                }
                await unitOfWork.Complete();
            }
            return true;
        }

    }
}
