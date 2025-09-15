using ClientDashboard_API.Data;
using ClientDashboard_API.Extensions;
using Microsoft.OpenApi.Models;

namespace ClientDashboard_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddApplicationServices(builder.Configuration);

            // CORS necessary when calling API from your GUI/domain
            // need to adjust origins later
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", b =>
                    b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClientDashboard API", Version = "v1" });
            });

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                await SeedClients.Seed(context);
            }

            // captures the bool set to EnableSwagger on azure
            // allowing or disallowing swagger in production

            var enableSwagger = app.Environment.IsDevelopment() ||
                builder.Configuration.GetValue<bool>("EnableSwagger");

            if (enableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClientDashboard API v1");
                    c.RoutePrefix = app.Environment.IsDevelopment() ? string.Empty : "swagger";
                });
            }

            // Enable serving static files from wwwroot
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}





