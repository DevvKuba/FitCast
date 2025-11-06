using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    // <DataContext> mathces what EF Core DI system provides
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<UserBase> Users { get; set; }
        public DbSet<Client> Client { get; set; }

        public DbSet<Trainer> Trainer { get; set; }

        public DbSet<Workout> Workouts { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Notification> Notification { get; set; }

        public DbSet<MonthlyTrainerRevenue> MonthlyTrainerRevenue { get; set; }

        public DbSet<ClientDailyFeature> ClientDailyFeature { get; set; }

        public DbSet<ClientChurnLabel> ClientChurnLabel { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserBase>().ToTable("Users");
            builder.Entity<Client>().ToTable("Clients");
            builder.Entity<Trainer>().ToTable("Trainers");
            builder.Entity<Payment>().ToTable("Payments");
            builder.Entity<MonthlyTrainerRevenue>().ToTable("MonthlyTrainerRevenue");
            builder.Entity<ClientDailyFeature>().ToTable("ClientDailyFeatures");
            builder.Entity<ClientChurnLabel>().ToTable("ClientChurnLabels");

            // explicit identiy configuration
            builder.Entity<UserBase>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn(1, 1);


            builder.Entity<Trainer>()
                .Property(t => t.AverageSessionPrice)
                .HasPrecision(18, 2);

            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.Entity<MonthlyTrainerRevenue>()
                .Property(m => m.AverageSessionPrice)
                .HasPrecision(18, 2);

            builder.Entity<MonthlyTrainerRevenue>()
                .Property(m => m.MonthlyRevenue)
                .HasPrecision(18, 2);

            builder.Entity<ClientDailyFeature>()
                .Property(c => c.LifeTimeValue)
                .HasPrecision(18, 2);

            // Client relationship
            builder.Entity<Client>()
                .HasMany(e => e.Workouts)
                .WithOne(e => e.Client) // reference in ClientWorkouts
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Client>()
                .HasMany<ClientDailyFeature>()
                .WithOne(c => c.Client)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);

            builder.Entity<Client>()
                .HasMany<ClientChurnLabel>()
                .WithOne(c => c.Client)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);

            // Trainer relationship
            builder.Entity<Trainer>()
                .HasMany(e => e.Clients)
                .WithOne(e => e.Trainer)
                .HasForeignKey(e => e.TrainerId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            builder.Entity<Trainer>()
                .HasMany<MonthlyTrainerRevenue>()
                .WithOne(t => t.Trainer)
                .HasForeignKey(t => t.TrainerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);

            // Nofitication relationships
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
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Payment relationships
            builder.Entity<Client>()
                .HasMany<Payment>()
                .WithOne(p => p.Client)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Trainer>()
                .HasMany<Payment>()
                .WithOne(p => p.Trainer)
                .HasForeignKey(p => p.TrainerId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(true);


        }

    }
}
