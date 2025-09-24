using System.Text.Json.Serialization;

namespace Client_Session_Tracker_C_.Models
{
    public class EventsModel
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("workout")]
        public WorkoutModel? Workout { get; set; }

        // Optional properties for deleted events
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("deleted_at")]
        public string? Deleted_At { get; set; }

    }
}
