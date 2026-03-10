using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // Fake token provider for testing
    public class FakeTokenProvider : ITokenProvider
    {
        public string Create(UserBase user)
        {
            // Return a fake token for testing
            return $"fake_token_for_{user.Email}";
        }
    }

    public class LoginServiceTests
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
        private readonly FakeTokenProvider _tokenProvider;
        private readonly LoginService _loginService;

        public LoginServiceTests()
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

            _tokenProvider = new FakeTokenProvider();
            _loginService = new LoginService(_unitOfWork, _tokenProvider, _passwordHasher);
        }

        [Fact]
        public async Task TestLoginServiceHandleSuccessfulTrainerLoginAsync()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hashedPassword = _passwordHasher.Hash(password);

            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword,
                EmailVerified = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "john.doe@example.com",
                Password = password,
                Role = UserRole.Trainer
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Token created successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(trainer.FirstName, result.Data.FirstName);
            Assert.Equal(trainer.Id, result.Data.Id);
            Assert.Equal(UserRole.Trainer, result.Data.Role);
            Assert.Equal($"fake_token_for_{trainer.Email}", result.Data.Token);
        }

        [Fact]
        public async Task TestLoginServiceHandleSuccessfulClientLoginAsync()
        {
            // Arrange
            var password = "ClientPassword456!";
            var hashedPassword = _passwordHasher.Hash(password);

            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "smith",
                Email = "trainer@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Email = "alice@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                PasswordHash = hashedPassword
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "alice@example.com",
                Password = password,
                Role = UserRole.Client
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Token created successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(client.FirstName, result.Data.FirstName);
            Assert.Equal(client.Id, result.Data.Id);
            Assert.Equal(UserRole.Client, result.Data.Role);
            Assert.Equal($"fake_token_for_{client.Email}", result.Data.Token);
        }

        [Fact]
        public async Task TestLoginServiceHandleReturnsErrorWhenUserNotFoundAsync()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("The user was not found", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestLoginServiceHandleReturnsErrorWhenPasswordIncorrectAsync()
        {
            // Arrange
            var correctPassword = "CorrectPassword123!";
            var hashedPassword = _passwordHasher.Hash(correctPassword);

            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword,
                EmailVerified = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "jane.smith@example.com",
                Password = "WrongPassword123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("The password is incorrect", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestLoginServiceHandleReturnsErrorWhenTrainerEmailNotVerifiedAsync()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hashedPassword = _passwordHasher.Hash(password);

            var trainer = new Trainer
            {
                FirstName = "bob",
                Surname = "jones",
                Email = "bob.jones@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword,
                EmailVerified = false // Not verified
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "bob.jones@example.com",
                Password = password,
                Role = UserRole.Trainer
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("You must verifiy your email, you can resend the verification below", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestLoginServiceHandleAllowsClientLoginWithoutEmailVerificationAsync()
        {
            // Arrange
            var password = "ClientPassword789!";
            var hashedPassword = _passwordHasher.Hash(password);

            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "brown",
                Email = "trainer2@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "charlie",
                Email = "charlie@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                PasswordHash = hashedPassword
                // No email verification required for clients
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "charlie@example.com",
                Password = password,
                Role = UserRole.Client
            };

            // Act
            var result = await _loginService.Handle(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Token created successfully", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task TestLoginServiceHandleGeneratesDifferentTokensForDifferentUsersAsync()
        {
            // Arrange
            var password = "Password123!";
            var hashedPassword = _passwordHasher.Hash(password);

            var trainer1 = new Trainer
            {
                FirstName = "user1",
                Surname = "test",
                Email = "user1@example.com",
                PhoneNumber = "+1111111111",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword,
                EmailVerified = true
            };
            var trainer2 = new Trainer
            {
                FirstName = "user2",
                Surname = "test",
                Email = "user2@example.com",
                PhoneNumber = "+2222222222",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword,
                EmailVerified = true
            };
            await _context.Trainer.AddRangeAsync(trainer1, trainer2);
            await _unitOfWork.Complete();

            var loginDto1 = new LoginDto
            {
                Email = "user1@example.com",
                Password = password,
                Role = UserRole.Trainer
            };
            var loginDto2 = new LoginDto
            {
                Email = "user2@example.com",
                Password = password,
                Role = UserRole.Trainer
            };

            // Act
            var result1 = await _loginService.Handle(loginDto1);
            var result2 = await _loginService.Handle(loginDto2);

            // Assert
            Assert.NotNull(result1.Data);
            Assert.NotNull(result2.Data);
            Assert.NotEqual(result1.Data.Token, result2.Data.Token);
            Assert.Equal($"fake_token_for_user1@example.com", result1.Data.Token);
            Assert.Equal($"fake_token_for_user2@example.com", result2.Data.Token);
        }
    }
}
