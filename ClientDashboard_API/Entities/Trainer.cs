using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Entities
{
    public class Trainer
    {
        [Key]
        public int Id { get; set; }

        public required string Email { get; set; } 

        public required string FirstName { get; set; }

        public required string Surname { get; set; }

        public required string PasswordHash { get; set; }

        public List<Client> Clients { get; set; } = [];
    }
}
