using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Data
{
    public static class SeedClients
    {
        public static async Task Seed(DataContext context)
        {
            var csvData = await File.ReadAllLinesAsync("clientSession.csv");
            Random random = new Random();

            var clientData = csvData.Select(x => x.Split(",")).ToList().Skip(1);

            foreach (var clientRecord in clientData)
            {
                var dateTime = DateTime.Now;
                var client = new WorkoutData
                {
                    // potentially seed name as lower case
                    Title = clientRecord[0],
                    SessionDate = DateOnly.FromDateTime(dateTime),
                    CurrentBlockSession = int.Parse(clientRecord[1]),
                    TotalBlockSessions = int.Parse(clientRecord[2]),
                    ExerciseCount = random.Next(6, 12)
                };

                if (!context.Data.Contains(client))
                {
                    context.Data.Add(client);
                }
            }

            await context.SaveChangesAsync();

        }
    }

}
