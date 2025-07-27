using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class WorkoutController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        // In workouts controller
        [HttpGet]
        public async Task<List<WorkoutDataDto>> GetAllDailyClientWorkouts(string date)
        {
            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsByDateAsync(DateOnly.Parse(date));
            var clientMappedSessions = new List<WorkoutDataDto>();

            if (clientSessions == null) throw new Exception($"No client sessions found on specificed date: {date}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDataDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return clientMappedSessions;

        }

        [HttpGet("{clientName}/lastDate")]
        public async Task<Workout> GetLatestClientWorkout(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkout(clientName);

            if (latestWorkoutInfo == null) throw new Exception($"{clientName} has no workouts recorded");

            return latestWorkoutInfo;

        }
    }
}
