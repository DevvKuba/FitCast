using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Entities
{
    public class WorkoutData
    {
        [Key]
        public required string Title { get; set; }

        public required DateOnly SessionDate { get; set; }

        // add TimeOnly SessionTime

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

        public int ExerciseCount { get; set; }

        // potentially add more data that can be stored 
    }
}
