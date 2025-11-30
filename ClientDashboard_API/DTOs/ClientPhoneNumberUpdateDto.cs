using System.Reflection.Metadata.Ecma335;

namespace ClientDashboard_API.DTOs
{
    public class ClientPhoneNumberUpdateDto
    {
        public required int Id { get; set; }

        public required string PhoneNumber { get; set; }
    }
}
