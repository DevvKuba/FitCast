using System.Text.Json.Serialization;

namespace Client_Session_Tracker_C_.Models
{
    public class ExerciseModel
    {
        [JsonPropertyName("index")]
        public required int Index { get; set; }

        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; } = null;

        [JsonPropertyName("exercise_template_id")]
        public string? Exercise_Template_Id { get; set; }

        [JsonPropertyName("superset_id")]
        public string? Superset_Id { get; set; }

        [JsonPropertyName("sets")]
        public required List<ExerciseInfoModel> Sets { get; set; }
    }
}