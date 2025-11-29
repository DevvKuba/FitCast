namespace Client_Session_Tracker_C_.Models
{
    public class ClientSessionModel
    {
        public required string Name { get; set; }

        public int CurrentSession { get; set; }

        public int BlockSessions { get; set; }

    }
}
