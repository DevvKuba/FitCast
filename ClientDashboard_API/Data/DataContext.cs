using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    // <DataContext> mathces what EF Core DI system provides
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<UserBase> Users { get; set; }
        public DbSet<Client> Client { get; set; }

        public DbSet<Workout> Workouts { get; set; }

        public DbSet<Trainer> Trainer { get; set; }

        public DbSet<Notification> Notification { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure inheritance
            builder.Entity<UserBase>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Trainer>("Trainer")
                .HasValue<Client>("Client");

            builder.Entity<Client>()
                .HasMany(e => e.Workouts)
                .WithOne(e => e.Client) // reference in ClientWorkouts
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Trainer>()
                .HasMany(e => e.Clients)
                .WithOne(e => e.Trainer)
                .HasForeignKey(e => e.TrainerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Client>()
                .HasMany<Notification>()
                .WithOne(n => n.Client)
                .HasForeignKey(n => n.ClientId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Trainer>()
                .HasMany<Notification>()
                .WithOne(n => n.Trainer)
                .HasForeignKey(n => n.TrainerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

        }

    }
}
