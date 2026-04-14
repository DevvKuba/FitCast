using ClientDashboard_API.Data;
using ClientDashboard_API.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ClientDashboard_API_Tests.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<ClientDashboard_API.Program>
    {
        private readonly SqliteConnection _connection;

        public CustomWebApplicationFactory()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["Jwt_Secret"] = "integration-tests-secret-key-1234567890",
                    ["Jwt_Issuer"] = "integration-tests",
                    ["Jwt_Audience"] = "integration-tests",
                    ["Email:SenderEmail"] = "integration@test.local",
                    ["Email:Sender"] = "Integration Test Sender",
                    ["Email:Host"] = "localhost",
                    ["Email:Port"] = "25",
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["EnableSwagger"] = "false"
                };

                configBuilder.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<DataContext>));
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<DataContext>));
                services.AddDbContext<DataContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IMessageService>();
                services.AddScoped<IMessageService, NoOpMessageService>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = IntegrationTestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = IntegrationTestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>(
                    IntegrationTestAuthHandler.SchemeName,
                    _ => { });

                var quartzHostedServiceDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService)
                        && d.ImplementationType?.FullName?.Contains("Quartz", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                foreach (var descriptor in quartzHostedServiceDescriptors)
                {
                    services.Remove(descriptor);
                }
            });
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
