using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    // <DataContext> mathces what EF Core DI system provides
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<Client> Client { get; set; }

        public DbSet<Workout> Workouts { get; set; }

        public DbSet<Trainer> Trainer { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Client>()
                .HasMany(e => e.Workouts)
                .WithOne(e => e.Client) // reference in ClientWorkouts
                .HasForeignKey(e => e.ClientId)
                .IsRequired();

            builder.Entity<Trainer>()
                .HasMany(e => e.Clients)
                .WithOne(e => e.Trainer)
                .HasForeignKey(e => e.TrainerId)
                .IsRequired(false);

        }

    }
}
