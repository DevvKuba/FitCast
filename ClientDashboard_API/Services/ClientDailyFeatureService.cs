using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using System.Reflection.Metadata.Ecma335;

namespace ClientDashboard_API.Services
{
    public class ClientDailyFeatureService(IUnitOfWork unitOfWork) : IClientDailyFeatureService
    {

        // set date as current date
        // sessions in 7d , declare a range , starting or current date-7 : current date how many have they completed return int
        // same process just for current date - 28 : current date

        // find their most recent session - days from currentdate

        // simply return int totalblocksessions - currentsession

        // gather client steps from current Date-1 : current Date - can be where the steps also get refreshed if called at 12am

        // for every workout linked to a client calculate mean of duration ** need to add as a propery still within models

        // LifeTimeValue from payments encapure all the *Confirmed* payments and their amounts under that client

        // return bool if status is currently active
        public async Task ExecuteClientDailyGatheringAsync(Client client)
        {
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var sessionInLast7Days = await unitOfWork.WorkoutRepository.GetSessionCountLast7DaysAsync(client, currentDate);
            var sessionsInLast28Days = await unitOfWork.WorkoutRepository.GetSessionCountLast28DaysAsync(client, currentDate);

            var daysSinceLastSession = await unitOfWork.WorkoutRepository.GetDaysFromLastSessionAsync(client, currentDate);
            // similar case may need to make RemainingSessions nullable since clients can actually have TotalBlockSessions as nullable
            var remainingSessions = client.TotalBlockSessions = client.CurrentBlockSession;

            // this one is nullable but should probably just be set to an int and decalred as 0

            var averageSessionDuration = await unitOfWork.WorkoutRepository.CalculateClientMeanWorkoutDurationAsync(client, currentDate);

            var lifeTimeValue = await unitOfWork.PaymentRepository.CalculateClientTotalLifetimeValueAsync(client, currentDate);

            var clientDailyDataInfo = new ClientDailyDataAddDto
            {
                AsOfDate = currentDate,
                SessionsIn7d = sessionInLast7Days,
                SessionsIn28d = sessionsInLast28Days,
                DaysSinceLastSession = daysSinceLastSession,
                RemainingSessions = remainingSessions,
                DailySteps = client.DailySteps,
                AverageSessionDuration = averageSessionDuration,
                LifeTimeValue = lifeTimeValue,
                CurrentlyActive = client.IsActive,
                ClientId = client.Id
            };

            await unitOfWork.ClientDailyFeatureRepository.AddNewRecordAsync(clientDailyDataInfo);
            await unitOfWork.Complete();
        }
    }
}
