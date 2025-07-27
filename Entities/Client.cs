using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Entities
{
    public class Client
    {
        [Key]
        public required string Name { get; set; }

        public int CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }

        public List<Workout> ClientWorkouts { get; set; } = [];
    }
}
