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
    // Fake email verification service for testing
    public class FakeEmailVerificationService : IEmailVerificationService
    {
        public List<string> SentEmails { get; } = new();
        public List<int> TrainerIdsProcessed { get; } = new();

        public Task CreateAndSendVerificationEmailAsync(Trainer trainer)
        {
            SentEmails.Add(trainer.Email);
            TrainerIdsProcessed.Add(trainer.Id);
            return Task.CompletedTask;
        }

        public Task<ApiResponseDto<string>> ProcessVerificationTokenAsync(string token)
        {
            throw new NotImplementedException();
        }

        public Task ResendVerificationEmailAsync(string email)
        {
            SentEmails.Add(email);
            return Task.CompletedTask;
        }
    }

    public class RegisterServiceTests
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
        private readonly FakeEmailVerificationService _emailVerificationService;
        private readonly RegisterService _registerService;

        public RegisterServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            }, global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
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
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            _emailVerificationService = new FakeEmailVerificationService();
            _registerService = new RegisterService(_unitOfWork, _passwordHasher, _emailVerificationService);
        }

        [Fact]
        public async Task TestRegisterServiceHandleSuccessfulTrainerRegistrationAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("john successfully added", result.Message);
            Assert.Equal("john", result.Data);

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByEmailAsync(registerDto.Email);
            Assert.NotNull(trainer);
            Assert.Equal("john", trainer.FirstName);
            Assert.Equal("doe", trainer.Surname);
            Assert.Equal("+1234567890", trainer.PhoneNumber); // Spaces removed
            Assert.Equal(UserRole.Trainer, trainer.Role);
            Assert.True(_passwordHasher.Verify(registerDto.Password, trainer.PasswordHash!));

            // Verify email verification was sent
            Assert.Single(_emailVerificationService.SentEmails);
            Assert.Contains(registerDto.Email, _emailVerificationService.SentEmails);
        }

        [Fact]
        public async Task TestRegisterServiceHandleRemovesSpacesFromPhoneNumberAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "+1 234 567 8900", // With spaces
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.True(result.Success);

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByEmailAsync(registerDto.Email);
            Assert.NotNull(trainer);
            Assert.Equal("+12345678900", trainer.PhoneNumber); // Spaces removed
        }

        [Fact]
        public async Task TestRegisterServiceHandleReturnsErrorWhenEmailAlreadyExistsAsync()
        {
            // Arrange
            var existingTrainer = new Trainer
            {
                FirstName = "existing",
                Surname = "user",
                Email = "duplicate@example.com",
                PhoneNumber = "+1111111111",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("Password123!")
            };
            await _context.Trainer.AddAsync(existingTrainer);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "new",
                Surname = "user",
                Email = "duplicate@example.com", // Duplicate email
                PhoneNumber = "+2222222222",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("The email is already in use", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestRegisterServiceHandleReturnsErrorWhenPhoneNumberAlreadyExistsAsync()
        {
            // Arrange
            var existingTrainer = new Trainer
            {
                FirstName = "existing",
                Surname = "user",
                Email = "user1@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("Password123!")
            };
            await _context.Trainer.AddAsync(existingTrainer);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "new",
                Surname = "user",
                Email = "user2@example.com",
                PhoneNumber = "+1234567890", // Duplicate phone number
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("The phone number is already is use", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestRegisterServiceHandleSuccessfulClientRegistrationAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "smith",
                Email = "trainer@example.com",
                PhoneNumber = "+9999999999",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "alice",
                Surname = "johnson",
                Email = "alice@example.com",
                PhoneNumber = "+1234567890",
                Password = "ClientPassword123!",
                ConfirmPassword = "ClientPassword123!",
                Role = UserRole.Client,
                ClientId = client.Id,
                ClientsTrainerId = trainer.Id
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Successfully registered as a client", result.Message);
            Assert.Equal("Success", result.Data);

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.NotNull(updatedClient);
            // Verify client details were updated (actual update logic depends on UpdateClientDetailsUponRegisterationAsync)
        }

        [Fact]
        public async Task TestRegisterServiceHandleReturnsErrorWhenClientIdIsNullAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "bob",
                Surname = "jones",
                Email = "bob@example.com",
                PhoneNumber = "+1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Client,
                ClientId = null, // Missing ClientId
                ClientsTrainerId = 1
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("clientId or clientsTrainerId are null fields", result.Message);
            Assert.Equal("Error", result.Data);
        }

        [Fact]
        public async Task TestRegisterServiceHandleReturnsErrorWhenClientsTrainerIdIsNullAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "charlie",
                Surname = "brown",
                Email = "charlie@example.com",
                PhoneNumber = "+1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Client,
                ClientId = 1,
                ClientsTrainerId = null // Missing ClientsTrainerId
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("clientId or clientsTrainerId are null fields", result.Message);
            Assert.Equal("Error", result.Data);
        }

        [Fact]
        public async Task TestMapClientDataUponRegistrationReturnsFalseWhenTrainerNotFoundAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "david",
                Surname = "miller",
                Email = "david@example.com",
                PhoneNumber = "+1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Client,
                ClientId = 1,
                ClientsTrainerId = 999 // Non-existent trainer
            };

            // Act
            var result = await _registerService.MapClientDataUponRegistrationAsync(registerDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestMapClientDataUponRegistrationReturnsFalseWhenClientNotFoundAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "davis",
                Email = "trainer3@example.com",
                PhoneNumber = "+8888888888",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "eve",
                Surname = "wilson",
                Email = "eve@example.com",
                PhoneNumber = "+1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Client,
                ClientId = 999, // Non-existent client
                ClientsTrainerId = trainer.Id
            };

            // Act
            var result = await _registerService.MapClientDataUponRegistrationAsync(registerDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestMapClientDataUponRegistrationReturnsTrueWhenBothExistAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "garcia",
                Email = "trainer4@example.com",
                PhoneNumber = "+7777777777",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "frank",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "frank",
                Surname = "martinez",
                Email = "frank@example.com",
                PhoneNumber = "+1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Client,
                ClientId = client.Id,
                ClientsTrainerId = trainer.Id
            };

            // Act
            var result = await _registerService.MapClientDataUponRegistrationAsync(registerDto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestRegisterServiceHashesPasswordCorrectlyAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "password",
                Surname = "test",
                Email = "password.test@example.com",
                PhoneNumber = "+5555555555",
                Password = "MySecurePassword456!",
                ConfirmPassword = "MySecurePassword456!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.True(result.Success);

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByEmailAsync(registerDto.Email);
            Assert.NotNull(trainer);
            Assert.NotNull(trainer.PasswordHash);
            Assert.NotEqual(registerDto.Password, trainer.PasswordHash); // Password should be hashed, not stored as plain text
            Assert.True(_passwordHasher.Verify(registerDto.Password, trainer.PasswordHash)); // Verify hash is correct
        }

        [Fact]
        public async Task TestRegisterServiceSetsEmailVerifiedToFalseForNewTrainerAsync()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "verify",
                Surname = "test",
                Email = "verify.test@example.com",
                PhoneNumber = "+4444444444",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Trainer
            };

            // Act
            var result = await _registerService.Handle(registerDto);

            // Assert
            Assert.True(result.Success);

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByEmailAsync(registerDto.Email);
            Assert.NotNull(trainer);
            Assert.False(trainer.EmailVerified); // Should be false initially
        }
    }
}

