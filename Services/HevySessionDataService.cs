using Client_Session_Tracker_C_.Models;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Interfaces;
using System.Text.Json;

namespace ClientDashboard_API.Services
{
    public class HevySessionDataService : ISessionDataParser
    {
        public string Api_Key { get; } = "7a610df4-9944-4f6f-ad60-bcb0450f5682";

        public async Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();

            // need to enable insensitivity so mapping can be done without worrying about casing
            var workoutsInfo = JsonSerializer.Deserialize<ApiSessionResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (workoutsInfo == null) throw new Exception("workouts not mapped correctly into models");

            // if type is "updated" proceed to tap into workout
            var workoutDetails = workoutsInfo.Events
                .Select(x => new WorkoutSummaryDto
                {
                    Title = x.Workout.Title,
                    SessionDate = DateOnly.Parse(x.Workout.Start_Time),
                    ExerciseCount = x.Workout.Exercises.Count,
                }).ToList();

            return workoutDetails;
        }

        public async Task<List<WorkoutSummaryDto>> CallApi()
        {
            DateTime todaysDate = DateTime.Now;

            // TESTING change logic later
            DateTime yesterdaysDate = todaysDate.AddDays(-1);
            // custom date formatter
            string desiredDate = yesterdaysDate.ToString("yyyy-MM-ddTHH:mmmm:ssZ");
            Console.WriteLine(desiredDate);

            string url = $"https://api.hevyapp.com/v1/workouts/events?page=1&pageSize=5&since={desiredDate}";

            // utilise HttpClient for requests
            using HttpClient client = new HttpClient();

            // Provide appropriate headers 
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("api-key", Api_Key);

            // call get request and retrieve response
            HttpResponseMessage response = await client.GetAsync(url);

            Console.WriteLine(response.StatusCode);

            // if reponse status code is 200 - successful proceed
            if (response.IsSuccessStatusCode)
            {
                return await RetrieveWorkouts(response);
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
    }
}
