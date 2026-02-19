using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.ML.Models
{
    public class ExtensionRecordsTracker
    {
        public required List<TrainerDailyRevenue> RevenueRecords { get; set; }

        public required TrainerDailyRevenue ExtendedFromRecord { get; set; }
    }
}
