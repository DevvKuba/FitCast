using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    // <DataContext> mathces what EF Core DI system provides
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<WorkoutData> Data { get; set; }

    }
}
