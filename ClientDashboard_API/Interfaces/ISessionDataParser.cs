using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ISessionDataParser
    {
        Task<List<WorkoutSummaryDto>> CallApiThroughPipelineAsync();

        Task<List<WorkoutSummaryDto>> CallApiForTrainerAsync(Trainer trainer);

        Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response);

        Task<bool> IsApiKeyValidAsync(string apiKey);

        int CalculateDurationInMinutes(string startTime, string endTime);

    }
}
