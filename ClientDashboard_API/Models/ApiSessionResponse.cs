using System.Text.Json.Serialization;

namespace Client_Session_Tracker_C_.Models
{
    public class ApiSessionResponse
    {
        [JsonPropertyName("events")]
        public required List<EventsModel> Events { get; set; }

    }
}
