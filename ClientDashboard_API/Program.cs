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
using Quartz.Listener;
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

            builder.WebHost.UseKestrel(options => options.AddServerHeader = false);

            builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

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

            if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddApplicationInsightsTelemetry();
            }

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

            // used https://www.youtube.com/watch?v=iD3jrj3RBuc as a means of support
            // when establishing first workout gathering job

            // Job keys are declared here (rather than inline inside AddQuartz) so the same
            // JobKey instances can be reused below to wire up the JobChainingJobListener once
            // the scheduler exists. Chain order: WorkoutSync -> InvalidTokenCleanup ->
            // InvisiblePaymentCleanup -> DeletedClientCleanup -> ClientDataGathering -> TrainerRevenueGathering
            var workoutSyncJobKey = new JobKey("DailyWorkoutSyncJob");
            var invalidTokenCleanupJobKey = new JobKey("DailyInvalidTokenCleanup");
            var invisiblePaymentCleanupJobKey = new JobKey("DailyInvisiblePaymentCleanup");
            var deletedClientCleanupJobKey = new JobKey("DailyDeletedClientCleanup");
            var clientDataJobKey = new JobKey("DailyClientDataGathering");
            var trainerRevenueJobKey = new JobKey("DailyTrainerRevenueGathering");

            builder.Services.AddQuartz(q =>
            {
                var timezone = TimeZoneInfo.Utc;

                // registering job for DI container
                q.AddJob<DailyTrainerWorkoutRetrieval>(opts => opts.WithIdentity(workoutSyncJobKey));

                // only the first job in the chain has a cron trigger - every job after it
                // is started by the JobChainingJobListener once the previous one finishes
                q.AddTrigger(opts => opts
                .ForJob(workoutSyncJobKey)
                .WithIdentity("DailyWorkoutSyncJob-trigger")
                .WithCronSchedule("2 11 0 * * ?", x => x
                .InTimeZone(timezone)
                .WithMisfireHandlingInstructionFireAndProceed())
                .WithDescription("Runs daily at midnight - 12:00AM to begin all background jobs")
                );


                // chain-triggered by DailyWorkoutSyncJob completing - no trigger of its own.
                // StoreDurably() is required here: Quartz normally drops triggerless jobs as
                // orphaned, so a job that's only ever started via TriggerJob (as the chain
                // listener does) has to opt out of that by declaring itself durable.
                q.AddJob<DailyInvalidTokenCleanup>(opts => opts.WithIdentity(invalidTokenCleanupJobKey).StoreDurably());


                // chain-triggered by DailyInvalidTokenCleanup completing - no trigger of its own
                q.AddJob<DailyInvisiblePaymentCleanup>(opts => opts.WithIdentity(invisiblePaymentCleanupJobKey).StoreDurably());


                // chain-triggered by DailyInvisiblePaymentCleanup completing - no trigger of its own
                q.AddJob<DailyDeletedClientCleanup>(opts => opts.WithIdentity(deletedClientCleanupJobKey).StoreDurably());


                // chain-triggered by DailyDeletedClientCleanup completing - no trigger of its own
                q.AddJob<DailyClientDataGathering>(opts => opts.WithIdentity(clientDataJobKey).StoreDurably());


                // chain-triggered by DailyClientDataGathering completing - no trigger of its own
                q.AddJob<DailyTrainerRevenueGathering>(opts => opts.WithIdentity(trainerRevenueJobKey).StoreDurably());
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

                if (app.Environment.IsEnvironment("Testing"))
                {
                    await context.Database.EnsureCreatedAsync();
                }
                else
                {
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
            }

            using (var scope = app.Services.CreateScope())
            {
                var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
                var scheduler = await schedulerFactory.GetScheduler();

                // registered globally: it only acts on jobs that have an explicit chain link below,
                // so it's safe even if more (unrelated) jobs are added to the scheduler later.
                // Chaining fires the next job regardless of whether the previous one succeeded or
                // threw - these five jobs are independent cleanup/gathering processes, not a
                // fail-fast pipeline, so that's the desired behaviour here, not an oversight.
                var dailyJobChainListener = new JobChainingJobListener("DailyJobChain");
                dailyJobChainListener.AddJobChainLink(workoutSyncJobKey, invalidTokenCleanupJobKey);
                dailyJobChainListener.AddJobChainLink(invalidTokenCleanupJobKey, invisiblePaymentCleanupJobKey);
                dailyJobChainListener.AddJobChainLink(invisiblePaymentCleanupJobKey, deletedClientCleanupJobKey);
                dailyJobChainListener.AddJobChainLink(deletedClientCleanupJobKey, clientDataJobKey);
                dailyJobChainListener.AddJobChainLink(clientDataJobKey, trainerRevenueJobKey);

                scheduler.ListenerManager.AddJobListener(dailyJobChainListener);
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

            app.UseExceptionHandler();

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





