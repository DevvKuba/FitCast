using ClientDashboard_API.Extensions;

namespace ClientDashboard_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddApplicationServices(builder.Configuration);

            var app = builder.Build();

            app.Run();
        }
    }
}

// program should fetch data from hevy api and populate the database accordingly 

// the the ClientDataController contains specific requests for that data within the db

// add cors
// implement seeding data from .csv to database for testing purposes
// create a mapping class, use Mapper


