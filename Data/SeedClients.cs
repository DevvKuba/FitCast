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
                var client = new Client
                {
                    // potentially seed name as lower case
                    Name = clientRecord[0].ToLower(),
                    CurrentBlockSession = int.Parse(clientRecord[1]),
                    TotalBlockSessions = int.Parse(clientRecord[2]),
                    ClientWorkouts = new List<Workout>()
                };

                if (!context.Client.Contains(client))
                {
                    context.Client.Add(client);
                }
            }

            await context.SaveChangesAsync();

        }
    }

}
