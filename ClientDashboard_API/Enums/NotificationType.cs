namespace ClientDashboard_API.Enums
{
    public enum NotificationType
    {
        TrainerBlockCompletionReminder,
        ClientBlockCompletionReminder,
        // for when a client is automatically added through Hevy workout retrieval
        NewClientConfigurationReminder,
        ClientStepsTrackedNotification,
        RetrievalWorkoutsCountNotification
    }
}
