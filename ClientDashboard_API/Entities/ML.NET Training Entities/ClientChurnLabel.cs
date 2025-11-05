namespace ClientDashboard_API.Entities.ML.NET_Training_Entities
{
    public class ClientChurnLabel
    {
        public int Id { get; set; }

        public int? ClientId { get; set; }

        public DateOnly AsOfDate { get; set; }

        public int ChurnedByDate { get; } = 28;
    }
}
