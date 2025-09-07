namespace Client_Session_Tracker_C_.Models
{
    public class WorkoutModel
    {
        public required string Title { get; set; }

        public required string Start_Time { get; set; }

        public required List<ExerciseModel> Exercises { get; set; }
    }
}
