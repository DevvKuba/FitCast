namespace ClientDashboard_API.Enums
{
    public enum NotificationType
    {
        TrainerBlockCompletionReminder = 1,
        ClientBlockCompletionReminder = 2,
        NewClientConfigurationReminder = 3, // for when a client is automatically added through Hevy workout retrieval
        ClientStepsTrackedNotification = 4,
        RetrievalWorkoutsCountNotification = 5
    }
}
