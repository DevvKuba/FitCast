using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class ClientDailyFeatureRepositoryTests
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
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;

        public ClientDailyFeatureRepositoryTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
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
            _userRepository = new UserRepository(_context);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository);
        }

        [Fact]
        public async Task TestAddNewRecordAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Role = "client",
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientDailyData = new ClientDailyDataAddDto
            {
                AsOfDate = DateOnly.Parse("15/06/2024"),
                SessionsIn7d = 3,
                SessionsIn28d = 10,
                DaysSinceLastSession = 2,
                RemainingSessions = 4,
                DailySteps = 8000,
                AverageSessionDuration = 45.5,
                LifeTimeValue = 500.00m,
                CurrentlyActive = true,
                ClientId = client.Id
            };

            await _clientDailyFeatureRepository.AddNewRecordAsync(clientDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.ClientDailyFeature.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Equal(DateOnly.Parse("15/06/2024"), savedRecord.AsOfDate);
            Assert.Equal(3, savedRecord.SessionsIn7d);
            Assert.Equal(10, savedRecord.SessionsIn28d);
            Assert.Equal(2, savedRecord.DaysSinceLastSession);
            Assert.Equal(4, savedRecord.RemainingSessions);
            Assert.Equal(8000, savedRecord.DailySteps);
            Assert.Equal(45.5, savedRecord.AverageSessionDuration);
            Assert.Equal(500.00m, savedRecord.LifeTimeValue);
            Assert.True(savedRecord.CurrentlyActive);
            Assert.Equal(client.Id, savedRecord.ClientId);
        }

        [Fact]
        public async Task TestAddNewRecordWithNullDaysSinceLastSessionAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Role = "client",
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientDailyData = new ClientDailyDataAddDto
            {
                AsOfDate = DateOnly.Parse("15/06/2024"),
                SessionsIn7d = 0,
                SessionsIn28d = 0,
                DaysSinceLastSession = null,
                RemainingSessions = 8,
                DailySteps = 5000,
                AverageSessionDuration = 0,
                LifeTimeValue = 0m,
                CurrentlyActive = true,
                ClientId = client.Id
            };

            await _clientDailyFeatureRepository.AddNewRecordAsync(clientDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.ClientDailyFeature.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Null(savedRecord.DaysSinceLastSession);
            Assert.Equal(8, savedRecord.RemainingSessions);
        }
    }
}
