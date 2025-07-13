namespace ClientDashboard_API.Dto_s
{
    public class WorkoutDataDto
    {
        public required string Name { get; set; }

        public required DateOnly SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

    }
}
