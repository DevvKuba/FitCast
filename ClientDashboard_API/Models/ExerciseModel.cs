namespace Client_Session_Tracker_C_.Models
{
    public class ExerciseModel
    {
        public required int Index { get; set; }
        public required string Title { get; set; }

        public string? Notes { get; set; } = null;

        public required List<ExerciseInfoModel> Sets { get; set; }


    }
}
