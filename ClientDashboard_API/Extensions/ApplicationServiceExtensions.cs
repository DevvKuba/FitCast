using ClientDashboard_API.Data;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace ClientDashboard_API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Add services to the container.
            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            });

            // whenever you need the Interface pass the class instead / utilise dependency injection, 
            // e.g. passing in IUserRepositary userRepository => allows for UserRepository functionality
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IWorkoutRepository, WorkoutRepository>();
            services.AddScoped<ITrainerRepository, TrainerRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
            services.AddScoped<IClientDailyFeatureRepository, ClientDailyFeatureRepository>();
            services.AddScoped<ITrainerDailyRevenueRepository, TrainerDailyRevenueRepository>();

            services.AddScoped<IVerifyEmail, VerifyEmail>();
            services.AddScoped<ISessionDataParser, HevySessionDataService>();
            services.AddScoped<ISessionSyncService, SessionSyncService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IMessageService, TwillioMessageService>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IAutoPaymentCreationService, AutoPaymentCreationService>();
            services.AddScoped<IClientDailyFeatureService, ClientDailyFeatureService>();
            services.AddScoped<ITrainerDailyRevenueService, TrainerDailyRevenueService>();
            services.AddScoped<IEmailVerificationLinkFactory, EmailVerificationLinkFactory>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IPasswordResetLinkFactory, PasswordResetLinkFactory>();

            services.AddSingleton<IApiKeyEncryter, ApiKeyEncrypter>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            return services;
        }
    }
}
