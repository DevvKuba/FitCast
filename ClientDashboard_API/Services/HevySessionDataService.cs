using Client_Session_Tracker_C_.Models;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Interfaces;
using System.Text.Json;

namespace ClientDashboard_API.Services
{
    public class HevySessionDataService : ISessionDataParser
    {
        public string API_KEY { set; get; } = Environment.GetEnvironmentVariable("API_KEY")!;

        public async Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();

            // add if workouts are empty... maybe check string length - if there are no workouts the strin will be / not be a specific length

            // need to enable insensitivity so mapping can be done without worrying about casing
            ApiSessionResponse? workoutsInfo = null;
            try
            {
                workoutsInfo = JsonSerializer.Deserialize<ApiSessionResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception)
            {
                Console.WriteLine($"No workouts present when gathering from the Hevy Api");
            }

            if (workoutsInfo == null) return [];

            // check issue with Parsing string "1970-01-01T00:00:00Z" to a DateOnly "1970-01-01"
            var workoutDetails = workoutsInfo.Events
                .Select(x => new WorkoutSummaryDto
                {
                    Title = x.Workout.Title,
                    SessionDate = DateOnly.Parse(x.Workout.Start_Time[0..10]),
                    ExerciseCount = x.Workout.Exercises.Count,
                }).ToList();

            return workoutDetails;
        }

        public async Task<List<WorkoutSummaryDto>> CallApi()
        {
            DateTime todaysDate = DateTime.Now;

            // TESTING change logic later - may need to have the time always be static to retrieve consistent results
            DateTime yesterdaysDate = todaysDate.AddDays(-1);
            // custom date formatter
            string desiredDate = yesterdaysDate.ToString("yyyy-MM-ddTHH:mmmm:ssZ");
            Console.WriteLine(desiredDate);

            string url = $"https://api.hevyapp.com/v1/workouts/events?page=1&pageSize=5&since={desiredDate}";

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
    }
}
