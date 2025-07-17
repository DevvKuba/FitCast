using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<WorkoutData, WorkoutDataDto>();
        }
    }
}
