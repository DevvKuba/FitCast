namespace ClientDashboard_API.Dto_s
{
    public class ClientUpdateDTO
    {
        public string? NewName { get; set; }
        public int? CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }
    }
}
