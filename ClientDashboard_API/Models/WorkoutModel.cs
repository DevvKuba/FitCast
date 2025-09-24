using System.Text.Json.Serialization;

namespace Client_Session_Tracker_C_.Models
{
    public class WorkoutModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("start_time")]
        public string? Start_Time { get; set; }

        [JsonPropertyName("exercises")]
        public List<ExerciseModel>? Exercises { get; set; }
    }
}
