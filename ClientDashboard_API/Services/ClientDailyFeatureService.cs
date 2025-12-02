using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class ClientDailyFeatureService : IClientDailyFeatureService
    {
        // needs to run for every client under a specific trainer
        // gather all trainers with their clients for each client..

        // set date as current date
        // sessions in 7d , declare a range , starting or current date-7 : current date how many have they completed return int
        // same process just for current date - 28 : current date

        // find their most recent session - days from currentdate

        // simply return int totalblocksessions - currentsession

        // gather client steps from current Date-1 : current Date - can be where the steps also get refreshed if called at 12am

        // for every workout linked to a client calculate mean of duration ** need to add as a propery still within models

        // LifeTimeValue from payments encapure all the *Confirmed* payments and their amounts under that client

        // return bool if status is currently active
        public Task ExecuteClientDailyGatheringAsync(Client client)
        {
            throw new NotImplementedException();
        }
    }
}
