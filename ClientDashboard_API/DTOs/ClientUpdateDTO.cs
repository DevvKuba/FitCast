namespace ClientDashboard_API.Dto_s
{
    public class ClientUpdateDto
    {
        public string? FirstName { get; set; }

        public bool? IsActive { get; set; }
        public int? CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }
    }
}
