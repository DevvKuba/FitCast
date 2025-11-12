using Client_Session_Tracker_C_.Models;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientDashboard_API.Services
{
    public class HevySessionDataService : ISessionDataParser
    {
        public string API_KEY { set; get; } = Environment.GetEnvironmentVariable("API_KEY")!;

        public async Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();

            // need to enable insensitivity so mapping can be done without worrying about casing
            ApiSessionResponse? workoutsInfo = null;
            // if no workouts are logged for the day, events array isn't present within the response
            // so structure can't de deserialised
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip // ignores extra properties
                };
                workoutsInfo = JsonSerializer.Deserialize<ApiSessionResponse>(json, options);
            }
            // possibly catch another type of exception to catch any bad requests
            catch (Exception)
            {
                Console.WriteLine($"No workouts present when gathering from the Hevy Api");
            }

            if (workoutsInfo == null) return [];

            var workoutDetails = workoutsInfo.Events
                .Where(x => x.Workout != null) // filters out deleted events
                .Select(x => new WorkoutSummaryDto
                {
                    Title = x.Workout!.Title ?? "Unknown",
                    SessionDate = DateOnly.Parse(x.Workout.Start_Time?[0..10] ?? "1970-01-01"),
                    ExerciseCount = x.Workout.Exercises?.Count ?? 0,
                }).ToList();

            return workoutDetails;
        }

        public async Task<List<WorkoutSummaryDto>> CallApiThroughPipelineAsync()
        {
            DateTime todaysDate = DateTime.Now;

            // TESTING change logic later - may need to have the time always be static to retrieve consistent results
            DateTime yesterdaysDate = todaysDate.AddDays(-1);
            // custom date formatter
            string desiredDate = yesterdaysDate.ToString("yyyy-MM-ddTHH:mmmm:ssZ");
            Console.WriteLine(desiredDate);

            string url = $"https://api.hevyapp.com/v1/workouts/events?page=1&pageSize=10&since={desiredDate}";

            // utilise HttpClient for requests
            using HttpClient client = new HttpClient();

            // Provide appropriate headers 
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("api-key", API_KEY);

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

        public async Task<List<WorkoutSummaryDto>> CallApiForTrainerAsync(Trainer trainer)
        {
            DateTime todaysDate = DateTime.Now;
            DateTime yesterdaysDate = todaysDate.AddDays(-1);
            string desiredDate = yesterdaysDate.ToString("yyyy-MM-ddTHH:mmmm:ssZ");

            string url = $"https://api.hevyapp.com/v1/workouts/events?page=1&pageSize=10&since={desiredDate}";

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("api-key", trainer.WorkoutRetrievalApiKey);

            HttpResponseMessage response = await client.GetAsync(url);

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
