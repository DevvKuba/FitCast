using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class PaymentControllerTests
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
        private readonly PaymentController _paymentController;

        public PaymentControllerTests()
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

            _paymentController = new PaymentController(_unitOfWork);
        }

        [Fact]
        public async Task TestGetClientPaymentsReturnsPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var result = await _paymentController.GetClientPaymentsAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Payment>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task TestGetClientPaymentsReturnsNotFoundAsync()
        {
            var result = await _paymentController.GetClientPaymentsAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetTrainerPaymentsReturnsPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var result = await _paymentController.GetTrainerPaymentsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Payment>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task TestGetTrainerPaymentsReturnsNotFoundAsync()
        {
            var result = await _paymentController.GetTrainerPaymentsAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdatePaymentInformationSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = false
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            var updateDto = new PaymentUpdateRequestDto
            {
                Id = payment.Id,
                Amount = 200.00m,
                Currency = "$",
                NumberOfSessions = 12,
                PaymentDate = "20/06/2024",
                Confirmed = true
            };

            var result = await _paymentController.UpdatePaymentInformationAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(payment.Id.ToString(), response.Data);

            var savedPayment = await _context.Payments.FindAsync(payment.Id);
            Assert.Equal(200.00m, savedPayment!.Amount);
            Assert.Equal("$", savedPayment.Currency);
            Assert.Equal(12, savedPayment.NumberOfSessions);
            Assert.True(savedPayment.Confirmed);
        }

        [Fact]
        public async Task TestUpdatePaymentInformationReturnsNotFoundAsync()
        {
            var updateDto = new PaymentUpdateRequestDto
            {
                Id = 999,
                Amount = 200.00m,
                Currency = "$",
                NumberOfSessions = 12,
                PaymentDate = "20/06/2024",
                Confirmed = true
            };

            var result = await _paymentController.UpdatePaymentInformationAsync(updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewTrainerPaymentSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "£" };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var paymentDto = new PaymentAddDto
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 150.00m,
                NumberOfSessions = 8,
                PaymentDate = "15/06/2024",
                Confirmed = true
            };

            var result = await _paymentController.AddNewTrainerPaymentAsync(paymentDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedPayment = await _context.Payments.FirstOrDefaultAsync();
            Assert.NotNull(savedPayment);
            Assert.Equal(150.00m, savedPayment.Amount);
            Assert.Equal(8, savedPayment.NumberOfSessions);
            Assert.True(savedPayment.Confirmed);
        }

        [Fact]
        public async Task TestAddNewTrainerPaymentReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var paymentDto = new PaymentAddDto
            {
                TrainerId = 999,
                ClientId = client.Id,
                Amount = 150.00m,
                NumberOfSessions = 8,
                PaymentDate = "15/06/2024",
                Confirmed = true
            };

            var result = await _paymentController.AddNewTrainerPaymentAsync(paymentDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewTrainerPaymentReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var paymentDto = new PaymentAddDto
            {
                TrainerId = trainer.Id,
                ClientId = 999,
                Amount = 150.00m,
                NumberOfSessions = 8,
                PaymentDate = "15/06/2024",
                Confirmed = true
            };

            var result = await _paymentController.AddNewTrainerPaymentAsync(paymentDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestDeleteTrainerPaymentSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            var result = await _paymentController.DeleteTrainerPaymentAsync(payment.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(payment.Id.ToString(), response.Data);

            var deletedPayment = await _context.Payments.FindAsync(payment.Id);
            Assert.Null(deletedPayment);
        }

        [Fact]
        public async Task TestDeleteTrainerPaymentReturnsNotFoundAsync()
        {
            var result = await _paymentController.DeleteTrainerPaymentAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestFilterClientPaymentsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = null,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = null,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var result = await _paymentController.FilterClientPaymentsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data);

            var remainingPayments = await _context.Payments.CountAsync(p => p.ClientId == null);
            Assert.Equal(0, remainingPayments);
        }

        [Fact]
        public async Task TestFilterClientPaymentsReturnsZeroWhenNoOldPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _unitOfWork.Complete();

            var result = await _paymentController.FilterClientPaymentsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data);
        }

        [Fact]
        public async Task TestFilterClientPaymentsReturnsNotFoundAsync()
        {
            var result = await _paymentController.FilterClientPaymentsAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}
