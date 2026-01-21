using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class UserRepositoryTests
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

        public UserRepositoryTests()
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
        }

        [Fact]
        public async Task TestGetUserByEmailForTrainerAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var user = await _userRepository.GetUserByEmailAsync("john@example.com");

            Assert.NotNull(user);
            Assert.Equal("john@example.com", user.Email);
            Assert.Equal("john", user.FirstName);
            Assert.Equal(UserRole.Trainer, user.Role);
            Assert.IsType<Trainer>(user);
        }

        [Fact]
        public async Task TestGetUserByEmailForClientAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Surname = "smith",
                Email = "rob@example.com",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var user = await _userRepository.GetUserByEmailAsync("rob@example.com");

            Assert.NotNull(user);
            Assert.Equal("rob@example.com", user.Email);
            Assert.Equal("rob", user.FirstName);
            Assert.Equal(UserRole.Client, user.Role);
            Assert.IsType<Client>(user);
        }

        [Fact]
        public async Task TestGetUserByEmailReturnsNullForNonExistentEmailAsync()
        {
            var user = await _userRepository.GetUserByEmailAsync("nonexistent@example.com");

            Assert.Null(user);
        }

        [Fact]
        public async Task TestGetUserByEmailReturnsCorrectUserWhenMultipleUsersExistAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            var client = new Client
            {
                FirstName = "rob",
                Surname = "smith",
                Email = "rob@example.com",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var trainerUser = await _userRepository.GetUserByEmailAsync("john@example.com");
            var clientUser = await _userRepository.GetUserByEmailAsync("rob@example.com");

            Assert.NotNull(trainerUser);
            Assert.NotNull(clientUser);
            Assert.Equal(UserRole.Trainer, trainerUser.Role);
            Assert.Equal(UserRole.Client, clientUser.Role);
            Assert.IsType<Trainer>(trainerUser);
            Assert.IsType<Client>(clientUser);
        }
    }
}
