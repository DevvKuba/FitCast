namespace ClientDashboard_API.DTOs
{
    public class TrainerUpdateDto
    {
        public required string FirstName { get; set; }

        public string? Surname { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? BusinessName { get; set; }

        public string? DefaultCurrency { get; set; }

        public decimal? AverageSessionPrice { get; set; }
    }
}
