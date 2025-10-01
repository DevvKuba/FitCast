using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Entities
{
    public class Client
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public int CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }

        public List<Workout> Workouts { get; set; } = [];

        public int? TrainerId { get; set; }

        // navigration properties
        public Trainer Trainer { get; set; } = null!;
    }
}
