using Client_Session_Tracker_C_.Models;
using ClientDashboard_API.Dto_s;

namespace ClientDashboard_API.Services
{
    public class SessionIncrementorService
    {
        // refactor for DB
        public List<WorkoutSummaryDto> DailyClientWorkouts { get; set; }

        public string Path { get; set; }

        public List<string> ClientList { get; set; } = [];

        public SessionIncrementorService(List<WorkoutSummaryDto> dailyWorkouts, string path)
        {
            DailyClientWorkouts = dailyWorkouts;
            Path = path;
        }

        public async Task IncrementClientSessions()
        {
            await ConstructClientList();

            if (DailyClientWorkouts.Count > 0)
            {
                foreach (var workout in DailyClientWorkouts)
                {
                    // if a name from the title, is present within our ClientList
                    // we want to retrieve that name utilise out maybe ?
                    var title = workout.Title.Split(" ");
                    string clientName = title[0];
                    if (ClientList.Contains(clientName))
                    {
                        await UpdateClientSession(clientName);
                    }
                }
            }
        }

        public async Task ConstructClientList()
        {
            var sessionsData = await LoadCsvData();

            foreach (var session in sessionsData)
            {
                ClientList.Add(session.Name);
            }

        }

        public async Task UpdateClientSession(string clientName)
        {
            var sessionsData = await LoadCsvData();

            //var clientRow = sessionsData.Where(x => x.Name == clientName).FirstOrDefault();
            //int updatedSessions = clientRow!.CurrentSession++;
            var line = new List<string> { "Client name, Current session, Sessions block" };
            File.WriteAllLines(Path, line);

            foreach (var session in sessionsData)
            {
                line.Clear();
                if (session.Name == clientName)
                {
                    session.CurrentSession++;
                    if (session.CurrentSession > session.BlockSessions)
                    {
                        session.CurrentSession = 1;
                    }

                }
                line.Add($"{session.Name}, {session.CurrentSession}, {session.BlockSessions}");
                File.AppendAllLines(Path, line);
            }

        }

        public async Task<List<ClientSessionModel>> LoadCsvData()
        {
            // read from csv file and gather all the client names in order to use in above method
            var csvData = await File.ReadAllLinesAsync(Path);
            var data = csvData.Skip(1);
            var sessionsData = data.Select(data => data.Split(","))
                .Select(record => new ClientSessionModel
                {
                    Name = record[0],
                    CurrentSession = int.Parse(record[1]),
                    BlockSessions = int.Parse(record[2])
                })
            .ToList();

            return sessionsData;
        }
    }
}
