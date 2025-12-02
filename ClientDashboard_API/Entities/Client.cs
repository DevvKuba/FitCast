namespace ClientDashboard_API.Entities
{
    public class Client : UserBase
    {
        public bool IsActive { get; set; } = true;

        public int CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }

        public int DailySteps { get; set; } = 0;

        public double? Weight { get; set; }


        public List<Workout> Workouts { get; set; } = [];

        public int? TrainerId { get; set; }

        // navigration properties
        public Trainer? Trainer { get; set; } = null;
    }
}
