namespace ClientDashboard_API.Dto_s
{
    public class ClientUpdateDto
    {
        public string? Name { get; set; }
        public int? CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }
    }
}
