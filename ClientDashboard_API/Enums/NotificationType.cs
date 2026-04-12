namespace ClientDashboard_API.Enums
{
    public enum NotificationType
    {
        TrainerBlockCompletionReminder = 1,
        ClientBlockCompletionReminder = 2, // client notification type
        NewClientConfigurationReminder = 3, // for when a client is automatically added through Hevy workout retrieval
        ClientStepsTrackedNotification = 4,
        AutoRetrievalWorkoutsCountNotification = 5,
        PendingPaymentCreatedAlert = 6

        // notifications around a payment of x amount being due / confirmed
    }
}
