using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Extensions;
using ClientDashboard_API.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
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
            
            builder.Services
                .AddFluentEmail(builder.Configuration["Email:SenderEmail"], builder.Configuration["Email:Sender"])
                .AddSmtpSender(builder.Configuration["Email:Host"], builder.Configuration.GetValue<int>("Email:Port"));

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

            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        // all validation error messages through data annotations
                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .SelectMany(e => e.Value!.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        // if at some point we want to create a combined messages
                        // string. join all the errors through a bullet point or ';' for instance

                        // custom api response
                        var apiResponse = new ApiResponseDto<string>
                        {
                            Data = null,
                            Message = errors.FirstOrDefault() ?? "Validation Failed",
                            Success = false
                        };

                        return new BadRequestObjectResult(apiResponse);
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
                .WithDescription("Runs daily at midnight - 11:45AM to sync Hevy workouts"));

                var clientDataJobKey = new JobKey("DailyClientDataGathering");

                q.AddJob<DailyClientDataGathering>(opts => opts.WithIdentity(clientDataJobKey));

                q.AddTrigger(opts => opts
                .ForJob(clientDataJobKey)
                .WithIdentity("DailyClientDataGathering-trigger")
                .WithCronSchedule("0 5 0 * * ?")
                .WithDescription("Runs daily 5 minutes past midnight - 12:05AM to gather Client Feature Data"));

                var trainerRevenueJobKey = new JobKey("DailyTrainerRevenueGathering");

                q.AddJob<DailyTrainerRevenueGathering>(opts => opts.WithIdentity(trainerRevenueJobKey));

                q.AddTrigger(opts => opts
                .ForJob(trainerRevenueJobKey)
                .WithIdentity("DailyTrainerRevenueGathering-trigger")
                .WithCronSchedule("0 10 0 * * ?")
                .WithDescription("Runs daily 10 minutes past midnight - 12:10AM to gather Trainer Revenue Data")); 

            });

            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            var app = builder.Build();

            // allows for http redirection from old / default to custom domain
            app.Use(async (context, next) =>
            {
                var host = context.Request.Host.Host;

                // Redirect from Azure default domain to custom domain
                if (host.EndsWith("azurewebsites.net", StringComparison.OrdinalIgnoreCase))
                {
                    var newUrl = $"https://fitcast.uk{context.Request.Path}{context.Request.QueryString}";
                    context.Response.Redirect(newUrl, permanent: true);
                    return;
                }

                await next();
            });

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

            // Enable serving static files from wwwroot with cache control
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Cache static assets (JS, CSS) for 1 year since they have hashes
                    if (ctx.File.Name.EndsWith(".js") || ctx.File.Name.EndsWith(".css"))
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
                    }
                    // Don't cache index.html
                    else if (ctx.File.Name == "index.html")
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache,no-store,must-revalidate");
                        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                        ctx.Context.Response.Headers.Append("Expires", "0");
                    }
                }
            });
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





