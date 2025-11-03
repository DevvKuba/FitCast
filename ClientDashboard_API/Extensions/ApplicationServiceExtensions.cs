using ClientDashboard_API.Data;
using ClientDashboard_API.Data.Migrations;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Add services to the container.
            services.AddControllers();
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            });

            // whenever you need the Interface pass the class instead / utilise dependency injection, 
            // e.g. passing in IUserRepositary userRepository => allows for UserRepository functionality
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IWorkoutRepository, WorkoutRepository>();
            services.AddScoped<ITrainerRepository, TrainerRepository>();
            services.AddScoped<ISessionDataParser, HevySessionDataService>();
            services.AddScoped<ISessionSyncService, SessionSyncService>();
            services.AddScoped<IMessageService, TwillioMessageService>();
            services.AddScoped<ITrainerRegisterService, TrainerRegisterService>();
            services.AddScoped<ITrainerLoginService, TrainerLoginService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            return services;
        }
    }
}
