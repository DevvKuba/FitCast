using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    // <DataContext> mathces what EF Core DI system provides
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<Client> Client { get; set; }

        public DbSet<Workout> Workouts { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Client>()
                .HasMany(e => e.Workouts)
                .WithOne(e => e.Client) // reference in ClientWorkouts
                .HasForeignKey(e => e.ClientName)
                .IsRequired();

        }

    }
}
