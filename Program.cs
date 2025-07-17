using ClientDashboard_API.Data;
using ClientDashboard_API.Extensions;

namespace ClientDashboard_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddApplicationServices(builder.Configuration);

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                await SeedClients.Seed(context);
            }

            app.Run();
        }
    }
}

// program should fetch data from hevy api and populate the database accordingly 

// the the ClientDataController contains specific requests for that data within the db

// testing

// use HevyApiCalls to fill database

// add cors




