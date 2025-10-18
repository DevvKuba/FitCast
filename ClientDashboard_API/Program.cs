using ClientDashboard_API.Data;
using ClientDashboard_API.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ClientDashboard_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSelectiveOrigins", b =>
                    b.AllowAnyMethod().AllowAnyHeader().WithOrigins("http://localhost:4200", "https://localhost:4200"));
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClientDashboard API", Version = "v1" });
            });

            builder.Services.AddAuthorization();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt_Secret"]!)),
                        ValidIssuer = builder.Configuration["Jwt_Issuer"],
                        ValidAudience = builder.Configuration["Jwt_Audience"],
                        ClockSkew = TimeSpan.Zero
                    };

                    o.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogWarning("JWT authentication failed: {Message}", ctx.Exception.Message);
                            return Task.CompletedTask;
                        }
                    };
                });

            var app = builder.Build();

            app.MapGet("/google45f9e3f493489c5e.html",
                () => Results.Content("google-site-verification: google45f9e3f493489c5e.html", "text/plain"));

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var hasMigrations = context.Database.GetMigrations().Any();
                if (hasMigrations)
                {
                    await context.Database.MigrateAsync(); // applies pending, creates DB if needed
                }
                else
                {
                    await context.Database.EnsureCreatedAsync(); // bootstrap database schema when no migrations exist
                }

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
            app.UseCors("AllowSelectiveOrigins");
            // Authentication should come before Authorization
            // both should run before .MapControllers
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}





