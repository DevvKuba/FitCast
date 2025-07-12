using ClientDashboard_API.Dto_s;

namespace ClientDashboard_API.Interfaces
{
    public interface ISessionDataParser
    {
        Task<List<WorkoutSummaryDto>> CallApi();

        Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response);


    }
}
