using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class ClientDataDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<WorkoutData> Data { get; set; }

    }
}
