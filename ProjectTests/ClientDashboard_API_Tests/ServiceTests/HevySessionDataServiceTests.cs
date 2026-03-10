using AutoMapper;
using Client_Session_Tracker_C_.Models;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class FakeApiKeyEncrypter : IApiKeyEncryter
    {
        public string Decrypt(string encryptedApiKey)
        {
            return $"decrypted_{encryptedApiKey}";
        }

        public string Encrypt(string apiKey)
        {
            return $"encrypted_{apiKey}";
        }
    }

    public class HevySessionDataServiceTests
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly EmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;
        private readonly FakeApiKeyEncrypter _encrypter;
        private readonly HevySessionDataService _hevySessionDataService;

        public HevySessionDataServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            });
            _mapper = config.CreateMapper();
            _passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _userRepository = new UserRepository(_context, _passwordHasher);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            _encrypter = new FakeApiKeyEncrypter();
            _hevySessionDataService = new HevySessionDataService(_encrypter);
        }

        [Fact]
        public async Task TestRetrieveWorkoutsDeserializesJsonResponseSuccessfullyAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Morning Workout"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": [
                                { ""index"": 0, ""title"": ""Bench Press"", ""sets"": [] },
                                { ""index"": 1, ""title"": ""Squat"", ""sets"": [] },
                                { ""index"": 2, ""title"": ""Deadlift"", ""sets"": [] }
                            ]
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            var workout = workouts[0];
            Assert.Equal("Morning Workout", workout.Title);
            Assert.Equal(new DateOnly(2025, 3, 10), workout.SessionDate);
            Assert.Equal(3, workout.ExerciseCount);
            Assert.Equal(60, workout.Duration); // 1 hour = 60 minutes
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesMultipleWorkoutsAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Workout 1"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:30:00Z"",
                            ""exercises"": [
                                { ""index"": 0, ""title"": ""Exercise 1"", ""sets"": [] },
                                { ""index"": 1, ""title"": ""Exercise 2"", ""sets"": [] }
                            ]
                        }
                    },
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Workout 2"",
                            ""start_time"": ""2025-03-11T10:00:00Z"",
                            ""end_time"": ""2025-03-11T11:15:00Z"",
                            ""exercises"": [
                                { ""index"": 0, ""title"": ""Exercise 3"", ""sets"": [] }
                            ]
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Equal(2, workouts.Count);

            var workout1 = workouts[0];
            Assert.Equal("Workout 1", workout1.Title);
            Assert.Equal(2, workout1.ExerciseCount);
            Assert.Equal(90, workout1.Duration); // 1.5 hours = 90 minutes

            var workout2 = workouts[1];
            Assert.Equal("Workout 2", workout2.Title);
            Assert.Equal(1, workout2.ExerciseCount);
            Assert.Equal(75, workout2.Duration); // 1 hour 15 min = 75 minutes
        }

        [Fact]
        public async Task TestRetrieveWorkoutsFiltersOutDeletedEventsAsync()
        {
            // Arrange
            // Real Hevy API deleted events have "workout": null and "deleted_at" timestamp
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Valid Workout"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": [
                                { ""index"": 0, ""title"": ""Test Exercise"", ""sets"": [] }
                            ]
                        }
                    },
                    {
                        ""type"": ""deleted_workout"",
                        ""id"": ""deleted_123"",
                        ""deleted_at"": ""2025-03-09T12:00:00Z"",
                        ""workout"": null
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts); // Only valid workout returned (deleted event filtered out)
            Assert.Equal("Valid Workout", workouts[0].Title);
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesNullTitleAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": null,
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": []
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal("Unknown", workouts[0].Title); // Default title when null
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesNullStartTimeAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Test Workout"",
                            ""start_time"": null,
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": []
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal(new DateOnly(1970, 1, 1), workouts[0].SessionDate); // Default date when null
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesNullExercisesAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Test Workout"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": null
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal(0, workouts[0].ExerciseCount); // 0 when null
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesEmptyEventsArrayAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": []
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Empty(workouts);
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesInvalidJsonGracefullyAsync()
        {
            // Arrange
            var invalidJsonResponse = @"{
                ""invalid"": ""structure""
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(invalidJsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Empty(workouts); // Returns empty list on deserialization failure
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesMalformedJsonAsync()
        {
            // Arrange
            var malformedJson = @"{ this is not valid json }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(malformedJson)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Empty(workouts); // Returns empty list on JSON parse failure
        }

        [Fact]
        public void TestCalculateDurationInMinutesCalculatesCorrectlyAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            var endTime = "2025-03-10T09:30:00Z";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(90, duration); // 1.5 hours = 90 minutes
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesNullStartTimeAsync()
        {
            // Arrange
            string? startTime = null;
            var endTime = "2025-03-10T09:00:00Z";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(0, duration);
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesNullEndTimeAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            string? endTime = null;

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(0, duration);
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesBothNullAsync()
        {
            // Arrange
            string? startTime = null;
            string? endTime = null;

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(0, duration);
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesEmptyStringsAsync()
        {
            // Arrange
            var startTime = "";
            var endTime = "";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(0, duration);
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesShortDurationAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            var endTime = "2025-03-10T08:15:00Z";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(15, duration); // 15 minutes
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesLongDurationAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            var endTime = "2025-03-10T12:30:00Z";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(270, duration); // 4.5 hours = 270 minutes
        }

        [Fact]
        public void TestCalculateDurationInMinutesRoundsDownToIntegerAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            var endTime = "2025-03-10T08:30:30Z"; // 30 minutes 30 seconds

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(30, duration); // Rounds down to 30 minutes (not 31)
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesCaseInsensitiveJsonAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""EVENTS"": [
                    {
                        ""TYPE"": ""workout"",
                        ""WORKOUT"": {
                            ""TITLE"": ""Case Test"",
                            ""START_TIME"": ""2025-03-10T08:00:00Z"",
                            ""END_TIME"": ""2025-03-10T09:00:00Z"",
                            ""EXERCISES"": [
                                { ""INDEX"": 0, ""TITLE"": ""Test Exercise"", ""SETS"": [] }
                            ]
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal("Case Test", workouts[0].Title);
        }

        [Fact]
        public async Task TestRetrieveWorkoutsIgnoresExtraPropertiesAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Test Workout"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T09:00:00Z"",
                            ""exercises"": [
                                { ""index"": 0, ""title"": ""Exercise"", ""sets"": [] }
                            ],
                            ""extra_property"": ""ignored"",
                            ""another_field"": 123
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal("Test Workout", workouts[0].Title);
        }

        [Fact]
        public async Task TestRetrieveWorkoutsExtractsDateCorrectlyFromTimestampAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Date Test"",
                            ""start_time"": ""2025-12-25T14:30:45Z"",
                            ""end_time"": ""2025-12-25T15:30:45Z"",
                            ""exercises"": []
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal(new DateOnly(2025, 12, 25), workouts[0].SessionDate); // Extracts date only (first 10 chars)
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesDifferentDateFormatsAsync()
        {
            // Arrange
            var startTime = "2025-03-10 08:00:00";
            var endTime = "2025-03-10 09:45:00";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(105, duration); // 1 hour 45 min = 105 minutes
        }

        [Fact]
        public async Task TestRetrieveWorkoutsHandlesZeroExercisesAsync()
        {
            // Arrange
            var jsonResponse = @"{
                ""events"": [
                    {
                        ""type"": ""workout"",
                        ""workout"": {
                            ""title"": ""Empty Workout"",
                            ""start_time"": ""2025-03-10T08:00:00Z"",
                            ""end_time"": ""2025-03-10T08:30:00Z"",
                            ""exercises"": []
                        }
                    }
                ]
            }";

            var httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(jsonResponse)
            };

            // Act
            var workouts = await _hevySessionDataService.RetrieveWorkouts(httpResponse);

            // Assert
            Assert.Single(workouts);
            Assert.Equal(0, workouts[0].ExerciseCount);
            Assert.Equal(30, workouts[0].Duration);
        }

        [Fact]
        public void TestCalculateDurationInMinutesHandlesSameStartAndEndTimeAsync()
        {
            // Arrange
            var startTime = "2025-03-10T08:00:00Z";
            var endTime = "2025-03-10T08:00:00Z";

            // Act
            var duration = _hevySessionDataService.CalculateDurationInMinutes(startTime, endTime);

            // Assert
            Assert.Equal(0, duration); // 0 minutes for same time
        }
    }
}
