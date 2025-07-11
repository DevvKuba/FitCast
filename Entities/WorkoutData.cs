using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Entities
{
    public class WorkoutData
    {
        // potentially set an autoIncrement Id as a primary key or don't utilise one
        [Key]
        public required string Title { get; set; }

        public required DateTime SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

        public int ExerciseCount { get; set; }
    }
}
