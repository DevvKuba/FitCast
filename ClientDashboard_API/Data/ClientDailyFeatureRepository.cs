using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class ClientDailyFeatureRepository(DataContext context) : IClientDailyFeatureRepository
    {
        public async Task AddNewRecord(ClientDailyDataAddDto clientData)
        {
            var clientDailyFeature = new ClientDailyFeature
            {
                AsOfDate = clientData.AsOfDate,
                SessionsIn7d = clientData.SessionsIn7d,
                SessionsIn28d = clientData.SessionsIn28d,
                DaysSinceLastSession = clientData.DaysSinceLastSession,
                RemainingSessions = clientData.RemainingSessions,
                DailySteps = clientData.DailySteps,
                AverageSessionDuration = clientData.AverageSessionDuration,
                LifeTimeValue = clientData.LifeTimeValue,
                CurrentlyActive = clientData.CurrentlyActive,
            };

            await context.ClientDailyFeature.AddAsync(clientDailyFeature);
        }
    }
}
