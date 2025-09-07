namespace Client_Session_Tracker_C_.Models
{
    public class ApiSessionResponse
    {
        //public required List<JsonElement> Events { get; set; }
        public required List<EventsModel> Events { get; set; }
    }
}
