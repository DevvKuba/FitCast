using AutoMapper;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class WorkoutController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        /// <summary>
        /// Workout request for the retrieval of all daily client sessions
        /// </summary>
        [HttpGet("/GetAllDailySessions")]
        public async Task<ActionResult<List<Workout>>> GetAllDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();
            var todaysDate = DateOnly.Parse(todaysDateString[0..10]);

            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(todaysDate);

            if (clientSessions == null) return NotFound($"No client sessions found on specificed date: {todaysDateString}");

            return Ok(clientSessions);

        }

        /// <summary>
        /// Workout request for retrieving a specific client workout, at a given date
        /// </summary>
        [HttpGet("{clientName}/{workoutDate}/GetWorkoutAtDate")]
        public async Task<ActionResult<Workout>> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null) return NotFound($"Client: {clientName}'s workout at {workoutDate} was not found.");

            return Ok(clientWorkout);
        }


        /// <summary>
        /// Workout request for retrieving a list of client workouts,
        /// from a given date
        /// </summary>
        [HttpGet("{workoutDate}/GetWorkoutsFromDate")]
        public async Task<ActionResult<List<Workout>>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate)
        {
            var clientWorkouts = await unitOfWork.WorkoutRepository.GetClientWorkoutsFromDateAsync(workoutDate);

            if (clientWorkouts.Count() == 0) return NotFound($"No client sessions found on specificed date: {workoutDate}");

            return Ok(clientWorkouts);
        }

        /// <summary>
        /// Workout request for the retrieval of a specific client's last workout
        /// </summary>
        [HttpGet("{clientName}/lastDate")]
        public async Task<ActionResult<Workout>> GetLatestClientWorkoutAsync(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkoutAsync(clientName);

            if (latestWorkoutInfo == null) return NotFound($"{clientName} has no workouts recorded");

            return Ok(latestWorkoutInfo);
        }

        /// <summary>
        /// Workout request for adding a workout for a specific client
        /// </summary>
        [HttpPost("/newWorkout")]
        public async Task<IActionResult> AddNewClientWorkoutAsync(string clientName, string workoutTitle, DateOnly workoutDate, int exerciseCount)
        {
            // may need to look into specific api response a bit more, use fields to actually create that workout object to add ?
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"Client: {clientName} not found");

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount);

            if (await unitOfWork.Complete()) return Ok($"Workout added for client: {clientName}");
            return BadRequest("Adding client unsuccessful");

        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<IActionResult> DeleteClientWorkoutAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null) return BadRequest($"Cannot find specified client: {clientName}'s workout at {workoutDate}");

            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSession(clientWorkout.Client);
            unitOfWork.WorkoutRepository.RemoveWorkout(clientWorkout);

            if (await unitOfWork.Complete()) return Ok($"{clientName}'s workout at {workoutDate} has been removed");
            return BadRequest("Removing client unsuccessful");

        }

    }
}
