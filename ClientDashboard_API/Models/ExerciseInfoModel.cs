using System.Text.Json.Serialization;

namespace Client_Session_Tracker_C_.Models
{
    public class ExerciseInfoModel
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("weight_kg")]
        public decimal? Weight_Kg { get; set; }

        [JsonPropertyName("reps")]
        public int? Reps { get; set; }

        [JsonPropertyName("distance_meters")]
        public decimal? Distance_Meters { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int? Duration_Seconds { get; set; }

        [JsonPropertyName("rpe")]
        public double? Rpe { get; set; } = null;

        [JsonPropertyName("custom_metric")]
        public object? Custom_Metric { get; set; }
    }
}