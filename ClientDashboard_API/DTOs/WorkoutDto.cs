namespace ClientDashboard_API.Dto_s
{
    public class WorkoutDto
    {
        public required string Title { get; set; }

        public required DateOnly SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

    }
}
