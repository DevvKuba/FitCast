using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public static class RestoreBackupData
    {
        public static async Task RestoreClientsAndWorkouts(DataContext context)
        {
            // Only run if database is empty
            if (await context.Client.AnyAsync())
            {
                Console.WriteLine("Data already exists, skipping restoration.");
                return;
            }

            Console.WriteLine("Restoring backup data from CSV...");


            var clientsData = new List<(string firstName, string? email, string? phone, bool isActive, int currentSession, int? totalSessions, int trainerId)>
            {
                ("alex", "", "+447538516489", true, 4, 4, 1),
                ("michael", "", "+447964012449", true, 2, 3, 1),
                ("nathan", "", "+447934326029", false, 3, 4, 1),
                ("jack", "", "+447500433003", true, 4, 4, 1),
                ("rachael", "", "+447576644874", true, 3, 4, 1),
                ("polly", "", "+447770752176", true, 2, 4, 1),
                ("syed", "", "+447446107779", false, 8, 8, 1),
                ("kuba", "", "", true, 1, 0, 1),
                ("mubarik", "", "+447310615475", false, 4, 4, 1),
                ("tyrell", "", "+447377929885", false, 1, 2, 1),
                ("pk", "", "+447301171359", true, 3, 8, 1),
            };

            foreach (var clientData in clientsData)
            {
                // Create UserBase entry
                var userBase = new Client
                {
                    FirstName = clientData.firstName,
                    Role = "client",
                    Email = clientData.email,
                    PhoneNumber = clientData.phone,
                    IsActive = clientData.isActive,
                    CurrentBlockSession = clientData.currentSession,
                    TotalBlockSessions = clientData.totalSessions,
                    TrainerId = clientData.trainerId,
                };

                context.Client.Add(userBase);
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"Restored {clientsData.Count} clients.");


            var workoutsData = new List<(string clientName, string title, DateOnly date, int exerciseCount)>
            {
                ("jack", "Jack Full Body 1", new DateOnly(2024, 9, 16), 7),
                ("michael", "Michael push focus upper routine", new DateOnly(2024, 9, 21), 6),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 9, 23), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 9, 23), 8),
                ("jack", "Jack Full Body 2", new DateOnly(2024, 9, 23), 8),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 9, 25), 4),
                ("polly", "Polly strength routine", new DateOnly(2024, 9, 25), 6),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 9, 28), 7),
                ("rachael", "Rachael Full body 1", new DateOnly(2024, 9, 28), 7),
                ("michael", "Michael pull focus upper routine", new DateOnly(2024, 9, 28), 8),
                ("mubarik", "Mubarik Full Body", new DateOnly(2024, 9, 29), 6),
                ("tyrell", "Tyrell Full Body", new DateOnly(2024, 9, 29), 7),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 9, 30), 8),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 9, 30), 7),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 2), 6),
                ("polly", "Polly strength routine", new DateOnly(2024, 10, 2), 7),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 10, 5), 7),
                ("rachael", "Rachael Full body 2", new DateOnly(2024, 10, 5), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 10, 4), 7),
                ("mubarik", "Mubarik Full Body", new DateOnly(2024, 10, 6), 5),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 7), 6),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 10, 7), 8),
                ("jack", "Jack Full Body 1", new DateOnly(2024, 10, 7), 6),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 9), 8),
                ("polly", "Polly strength routine", new DateOnly(2024, 10, 9), 7),
                ("kuba", "Kuba Lower Body 1", new DateOnly(2024, 10, 9), 7),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 10, 11), 7),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 10, 12), 6),
                ("rachael", "Rachael Full body 1", new DateOnly(2024, 10, 12), 6),
                ("michael", "Michael push focus upper routine", new DateOnly(2024, 10, 12), 7),
                ("mubarik", "Mubarik Full Body 1", new DateOnly(2024, 10, 13), 6),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 14), 6),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 10, 14), 7),
                ("polly", "Polly strength routine", new DateOnly(2024, 10, 16), 6),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 17), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 10, 18), 8),
                ("kuba", "Kuba Lower Body 1", new DateOnly(2024, 10, 20), 4),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 20), 6),
                ("michael", "Michael push focus upper routine", new DateOnly(2024, 10, 20), 8),
                ("syed", "Syed Upper Routine", new DateOnly(2024, 10, 22), 6),
                ("mubarik", "Mubarik Full Body 1", new DateOnly(2024, 10, 22), 6),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 10, 22), 6),
                ("jack", "Jack Full Body 1", new DateOnly(2024, 10, 22), 7),
                ("kuba", "Kuba Lower Body 1", new DateOnly(2024, 10, 23), 5),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 10, 25), 7),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 10, 29), 8),
                ("jack", "Jack Full Body 2", new DateOnly(2024, 10, 29), 6),
                ("kuba", "Kuba Lower Body 1", new DateOnly(2024, 10, 30), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 11, 1), 7),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 11, 2), 7),
                ("michael", "Michael push focus upper routine", new DateOnly(2024, 11, 2), 8),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 11, 4), 6),
                ("polly", "Polly strength routine", new DateOnly(2024, 11, 4), 6),
                ("jack", "Jack Full Body 1", new DateOnly(2024, 11, 5), 6),
                ("polly", "Polly strength routine", new DateOnly(2024, 11, 6), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 11, 8), 6),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 11, 9), 7),
                ("rachael", "Rachael Full body 1", new DateOnly(2024, 11, 9), 6),
                ("polly", "Polly strength routine", new DateOnly(2024, 11, 10), 6),
                ("jack", "Jack Full Body", new DateOnly(2024, 11, 12), 5),
                ("kuba", "Kuba Upper Body 2", new DateOnly(2024, 11, 12), 5),
                ("kuba", "Kuba Lower Body 1", new DateOnly(2024, 11, 13), 6),
                ("pk", "Pk Full Body Routine", new DateOnly(2024, 11, 13), 6),
                ("kuba", "Kuba Upper Body 1", new DateOnly(2024, 11, 15), 7),
                ("pk", "Pk Lower Body Routine", new DateOnly(2024, 11, 15), 7),
                ("kuba", "Kuba Lower Body 2", new DateOnly(2024, 11, 16), 7),
                ("rachael", "Rachael Full body 1", new DateOnly(2024, 11, 16), 7),
                ("pk", "Pk Upper Body Routine", new DateOnly(2024, 11, 18), 7),
                ("alex", "Alex Upper Routine", new DateOnly(2024, 11, 18), 5),
            };

            foreach (var workoutData in workoutsData)
            {
                // Find the client by name
                var client = await context.Client
                    .FirstOrDefaultAsync(c => c.FirstName == workoutData.clientName);

                if (client != null)
                {
                    var workout = new Workout
                    {
                        ClientName = workoutData.clientName,
                        WorkoutTitle = workoutData.title,
                        SessionDate = workoutData.date,
                        ExerciseCount = workoutData.exerciseCount,
                        ClientId = client.Id,
                        Client = client
                    };

                    context.Workouts.Add(workout);
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"Restored {workoutsData.Count} workouts.");
            Console.WriteLine("Backup restoration complete!");
        }
    }
}