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
    public class AutoPaymentCreationServiceTests
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
        private readonly AutoPaymentCreationService _autoPaymentCreationService;

        public AutoPaymentCreationServiceTests()
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
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            _autoPaymentCreationService = new AutoPaymentCreationService(_unitOfWork);
        }

        [Fact]
        public async Task TestCreatePendingPaymentCreatesPaymentSuccessfullyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Email = "alice@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal($"Successfully created payment for {client.FirstName}", result.Message);

            // Verify payment was created in database
            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(trainer.Id, payment.TrainerId);
            Assert.Equal(client.Id, payment.ClientId);
            Assert.Equal(400.00m, payment.Amount); // 8 sessions * $50 = $400
            Assert.False(payment.Confirmed); // Should be pending (not confirmed)
        }

        [Fact]
        public async Task TestCreatePendingPaymentCalculatesBlockPriceCorrectlyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 75.50m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "bob",
                Email = "bob@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 10
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(755.00m, payment.Amount); // 10 sessions * $75.50 = $755.00
        }

        [Fact]
        public async Task TestCreatePendingPaymentSetsConfirmedToFalseAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "charlie",
                Surname = "brown",
                Email = "charlie.brown@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 60.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "david",
                Email = "david@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 6
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.False(payment.Confirmed); // Payment should be pending (not confirmed)
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesNullTotalBlockSessionsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "eve",
                Surname = "wilson",
                Email = "eve.wilson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "frank",
                Email = "frank@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = null // Null sessions
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(0.00m, payment.Amount); // null * $50 = 0
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesNullAverageSessionPriceAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "grace",
                Surname = "martinez",
                Email = "grace.martinez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = null // Null price
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "henry",
                Email = "henry@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(0.00m, payment.Amount); // 8 * null = 0
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesBothNullValuesAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "iris",
                Surname = "garcia",
                Email = "iris.garcia@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = null // Null price
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "jack",
                Email = "jack@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = null // Null sessions
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(0.00m, payment.Amount); // null * null = 0
        }

        [Fact]
        public async Task TestCreatePendingPaymentUsesTodaysDateAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "kate",
                Surname = "anderson",
                Email = "kate.anderson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 55.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "leo",
                Email = "leo@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.Now);

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(today, payment.PaymentDate);
        }

        [Fact]
        public async Task TestCreatePendingPaymentCanCreateMultiplePaymentsForSameClientAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "mike",
                Surname = "davis",
                Email = "mike.davis@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "nancy",
                Email = "nancy@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act - Create payment twice (e.g., for different blocks)
            var result1 = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);
            var result2 = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Equal(2, payments.Count); // Two separate payments
            Assert.All(payments, p =>
            {
                Assert.Equal(trainer.Id, p.TrainerId);
                Assert.Equal(client.Id, p.ClientId);
                Assert.Equal(400.00m, p.Amount);
                Assert.False(p.Confirmed);
            });
        }

        [Fact]
        public async Task TestCreatePendingPaymentCreatesPaymentsForMultipleClientsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "oscar",
                Surname = "rodriguez",
                Email = "oscar.rodriguez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 60.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client
            {
                FirstName = "peter",
                Email = "peter@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            var client2 = new Client
            {
                FirstName = "quinn",
                Email = "quinn@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 10
            };
            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            // Act
            var result1 = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client1);
            var result2 = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Equal(2, payments.Count);

            var payment1 = payments.First(p => p.ClientId == client1.Id);
            var payment2 = payments.First(p => p.ClientId == client2.Id);

            Assert.Equal(480.00m, payment1.Amount); // 8 * $60 = $480
            Assert.Equal(600.00m, payment2.Amount); // 10 * $60 = $600
        }

        [Fact]
        public async Task TestCreatePendingPaymentReturnsCorrectSuccessMessageAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "rita",
                Surname = "wilson",
                Email = "rita.wilson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "sam",
                Email = "sam@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully created payment for sam", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesZeroSessionsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "tom",
                Surname = "hernandez",
                Email = "tom.hernandez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "uma",
                Email = "uma@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 0 // Zero sessions
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(0.00m, payment.Amount); // 0 * $50 = $0
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesZeroSessionPriceAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "victor",
                Surname = "garcia",
                Email = "victor.garcia@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 0.00m // Free sessions
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "wendy",
                Email = "wendy@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(0.00m, payment.Amount); // 8 * $0 = $0
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesLargeBlockSizesAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "xander",
                Surname = "martinez",
                Email = "xander.martinez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 100.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "yara",
                Email = "yara@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 50 // Large block
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(5000.00m, payment.Amount); // 50 * $100 = $5000
        }

        [Fact]
        public async Task TestCreatePendingPaymentHandlesDecimalSessionPricesAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "zane",
                Surname = "anderson",
                Email = "zane.anderson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                AverageSessionPrice = 45.75m // Decimal price
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "amy",
                Email = "amy@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                TotalBlockSessions = 12
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            var result = await _autoPaymentCreationService.CreatePendingPaymentAsync(trainer, client);

            // Assert
            Assert.True(result.Success);

            var payments = await _context.Payments.ToListAsync();
            Assert.Single(payments);

            var payment = payments[0];
            Assert.Equal(549.00m, payment.Amount); // 12 * $45.75 = $549.00
        }
    }
}
