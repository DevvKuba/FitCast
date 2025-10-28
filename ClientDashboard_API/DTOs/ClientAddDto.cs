namespace ClientDashboard_API.DTOs
{
    public class ClientAddDto
    {
        public required string Name { get; set; }

        public required int TotalBlockSessions { get; set; }

        public required int TrainerId { get; set; }
    }
}
