using ClientDashboard_API.Data;
using ClientDashboard_API.Extensions;
using ClientDashboard_API.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Globalization;
using System.Text;

namespace ClientDashboard_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSelectiveOrigins", b =>
                {
                    b.WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200",
                        "https://fitcast.uk",
                        "https://www.fitcast.uk"
                        )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGenAuth();

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

            builder.Services.AddQuartz(q =>
            {

                // key for background job
                var workoutSyncJobKey = new JobKey("DailyWorkoutSyncJob");

                // registering job for DI container
                q.AddJob<DailyTrainerWorkoutRetrieval>(opts => opts.WithIdentity(workoutSyncJobKey));

                // setting up scheduled job trigger for midnight execution
                q.AddTrigger(opts => opts
                .ForJob(workoutSyncJobKey)
                .WithIdentity("DailyWorkoutSyncJob-trigger")
                .WithCronSchedule("0 0 0 * * ?")
                .WithDescription("Runs daily at midnight - 12AM to sync Hevy workouts"));

                var clientDataJobKey = new JobKey("DailyClientDataGathering");

                q.AddJob<DailyClientDataGathering>(opts => opts.WithIdentity(clientDataJobKey));

                q.AddTrigger(opts => opts
                .ForJob(clientDataJobKey)
                .WithIdentity("DailyClientDataGathering-trigger")
                .WithCronSchedule("0 5 0 * * ?")
                .WithDescription("Runs daily 5 minutes past midnight - 12:05AM to gather Client Feature Data"));

            });

            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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

            // Fallback to Angular SPA ONLY for non-API routes
            // This prevents /api/* routes from being caught by the SPA fallback
            app.MapFallback(context =>
            {
                // Don't intercept API calls
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                }

                // Serve Angular index.html for all other routes
                context.Response.ContentType = "text/html";
                return context.Response.SendFileAsync("wwwroot/index.html");
            });
            app.Run();
        }
    }
}





