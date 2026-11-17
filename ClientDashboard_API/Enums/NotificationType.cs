namespace ClientDashboard_API.Enums
{
    public enum NotificationType
    {
        TrainerBlockCompletionReminder = 1,
        ClientBlockCompletionReminder = 2, 
        NewClientConfigurationReminder = 3,
        ClientStepsTrackedNotification = 4,
        DailySessionSyncNotification = 5,
        PendingPaymentCreatedAlert = 6,
        QuickAddWorkoutReminder = 7,

        // notifications around a payment of x amount being due / confirmed
    }
}
