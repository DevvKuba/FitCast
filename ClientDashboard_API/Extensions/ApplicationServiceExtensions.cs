using Azure.Identity;
using ClientDashboard_API.Data;
using ClientDashboard_API.Exceptions;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Services;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Quartz;

namespace ClientDashboard_API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment environment
            )
        {
            // Add services to the container.
            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            });

            var provider = config["ModelStore:Provider"] ?? "Local";

            services.AddAzureClients(clients =>
            {
                if(provider == "Blob")
                {
                    clients.AddBlobServiceClient(new Uri(config["ModelStore:BlobAccountUrl"]!));
                    clients.UseCredential(new DefaultAzureCredential());
                }
                else if(provider == "Azurite")
                {
                    clients.AddBlobServiceClient("UseDevelopmentStorage=true");
                }
                
            });

            if (provider == "Local")
            {
                services.AddScoped<IModelStore, LocalFileModelStore>(); 
            }
            else
            {
                services.AddScoped<IModelStore, BlobModelStore>();
            }


            // whenever you need the Interface pass the class instead / utilise dependency injection, 
            // e.g. passing in IUserRepositary userRepository => allows for UserRepository functionality
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IWorkoutRepository, WorkoutRepository>();
            services.AddScoped<ITrainerRepository, TrainerRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationRecipientStatusRepository, NotificationRecipientStatusRepository>();
            services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
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
            services.AddScoped<IClientBlockTerminationHelper, ClientBlockTerminationHelper>();

            services.AddScoped<IMLModelTrainingService, TrainerRevenueMLTrainingService>();
            services.AddScoped<IMLPredictionService, TrainerRevenueMLPredictionService>();
            services.AddScoped<ITrainerFullMonthAnalyticsService, TrainerFullMonthAnalyticsService>();
            services.AddScoped<ITrainerCurrentMonthAnalyticsService, TrainerCurrentMonthAnalyticsService>();

            services.AddSingleton<IApiKeyEncryter, ApiKeyEncrypter>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            services.AddExceptionHandler<GlobalExceptionHandler>();

            services.AddAutoMapper(_ => { }, typeof(ApplicationServiceExtensions).Assembly);

            return services;
        }
    }
}
