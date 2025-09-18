using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Data
{
    public static class SeedClients
    {
        public static async Task Seed(DataContext context)
        {
            const string fileName = "clientSessions.csv";
            if (!File.Exists(fileName)) return; // no seed file in production.publish; skip

            var csvData = await File.ReadAllLinesAsync(fileName);
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
                    Workouts = new List<Workout>()
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
