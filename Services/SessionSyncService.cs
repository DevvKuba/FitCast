using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService : ISessionSyncService
    {
        // consider having all the HevyApi logic in a seperate project in itself
        public async Task<bool> SyncDailySessions(ISessionDataParser hevyParser)
        {
            // gathers all the data 
            var dailyWorkouts = await hevyParser.CallApi();

            if (dailyWorkouts == null) return false;

            return true;
            // if daily Workouts are empty just return ok "No daily workout sessions"


            // match the names to current clients in database
            // if they are not present they are to be added as a new record with starting session 1 - block null - can be later defined
        }

    }
}
