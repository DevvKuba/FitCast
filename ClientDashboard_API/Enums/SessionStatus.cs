namespace ClientDashboard_API.Enums
{
    public enum SessionStatus
    {
        Scheduled, // upcoming, or finished but not yet processed by the nightly sync
        Completed, // finished and processed, but not counted (the trainer has calendar sync off)
        Cancelled, // cancelled by the trainer, or its synced workout was deleted - never counted
        Synced // finished and counted: a workout was created and the client's block count increased

    }
}
