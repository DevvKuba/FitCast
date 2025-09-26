using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // TODO Instead of one below?
            //CreateMap<Workout, WorkoutDto>();
            CreateMap<Client, WorkoutDto>();
            CreateMap<ClientUpdateDto, Client>();
        }
    }
}
