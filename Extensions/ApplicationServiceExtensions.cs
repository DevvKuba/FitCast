using ClientDashboard_API.Data;
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
            services.AddDbContext<ClientDataDbContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            });

            // whenever you need the Interface pass the class instead / utilise dependency injection, 
            // e.g. passing in IUserRepositary userRepository => allows for UserRepository functionality
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IClientDataRepository, ClientDataRepository>();
            services.AddScoped<ISessionDataParser, HevySessionDataParser>();

            return services;
        }
    }
}
