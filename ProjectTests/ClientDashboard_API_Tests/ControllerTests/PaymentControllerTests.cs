using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Http;
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
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public PaymentControllerTests()
        {
            _mapper = TestMapperFactory.Create();
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
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context, _mapper);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            var (authorizationService, currentUserAccessor, httpContextAccessor) =
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler(), new PaymentOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _paymentController = new PaymentController(_unitOfWork, authorizationService, currentUserAccessor);
            TestAuthHelpers.AttachHttpContext(_paymentController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);
        private void AuthenticateAsClient(int clientId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Client", clientId);

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

            AuthenticateAsClient(client.Id);
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
        public async Task TestGetClientPaymentsReturnsForbiddenForDifferentClientAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var otherClient = new Client { FirstName = "sam", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddRangeAsync(client, otherClient);
            await _unitOfWork.Complete();

            AuthenticateAsClient(otherClient.Id);
            var result = await _paymentController.GetClientPaymentsAsync(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetTrainerPaymentsReturnsPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var activeClient = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [], IsDeleted = false };
            var deletedClient = new Client { FirstName = "jane", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [], IsDeleted = true };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(activeClient);
            await _context.Client.AddAsync(deletedClient);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = activeClient.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = deletedClient.Id,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _paymentController.GetTrainerPaymentsAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Payment>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
            Assert.Contains(response.Data, p => p.ClientId == activeClient.Id);
            Assert.Contains(response.Data, p => p.ClientId == deletedClient.Id);
        }

        [Fact]
        public async Task TestGetTrainerPaymentsReturnsNotFoundAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _paymentController.GetTrainerPaymentsAsync();
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

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestUpdatePaymentInformationReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = owningTrainer.Id,
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

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _paymentController.UpdatePaymentInformationAsync(updateDto);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedPayment = await _context.Payments.FindAsync(payment.Id);
            Assert.Equal(100.00m, savedPayment!.Amount);
        }

        [Fact]
        public async Task TestAddNewTrainerPaymentSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "£" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
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

            AuthenticateAsTrainer(trainer.Id);
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
            Assert.Equal(trainer.Id, savedPayment.TrainerId);
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

            // The caller's own id is what's resolved to a trainer now, not paymentDto.TrainerId - authenticate
            // as an id with no matching Trainer row to exercise this branch.
            AuthenticateAsTrainer(999);
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

            AuthenticateAsTrainer(trainer.Id);
            var result = await _paymentController.AddNewTrainerPaymentAsync(paymentDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewTrainerPaymentReturnsForbiddenWhenClientBelongsToAnotherTrainerAsync()
        {
            // Proves the AddNewTrainerPaymentAsync payload-trust gap is closed: the caller's own identity
            // builds the Payment.TrainerId now, and the client-ownership check stops a caller from adding a
            // payment against a client that isn't theirs, regardless of what paymentDto.TrainerId claims.
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "£" };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer, DefaultCurrency = "£" };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = UserRole.Client, TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var paymentDto = new PaymentAddDto
            {
                TrainerId = otherTrainer.Id,
                ClientId = client.Id,
                Amount = 150.00m,
                NumberOfSessions = 8,
                PaymentDate = "15/06/2024",
                Confirmed = true
            };

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _paymentController.AddNewTrainerPaymentAsync(paymentDto);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            Assert.False(await _context.Payments.AnyAsync());
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
                IsVisible = true,
                Confirmed = true
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _paymentController.DeleteTrainerPaymentAsync(payment.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(payment.Id.ToString(), response.Data);

            var deletedPayment = await _context.Payments.FindAsync(payment.Id);
            Assert.False(deletedPayment!.IsVisible);
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
        public async Task TestDeleteTrainerPaymentReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = owningTrainer.Id,
                ClientId = client.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                IsVisible = true,
                Confirmed = true
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _paymentController.DeleteTrainerPaymentAsync(payment.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var survivingPayment = await _context.Payments.FindAsync(payment.Id);
            Assert.True(survivingPayment!.IsVisible);
        }

        [Fact]
        public async Task TestFilterClientPaymentsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var activeClient = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [], TrainerId = trainer.Id, IsDeleted = false };
            var deletedClient = new Client { FirstName = "jane", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [], TrainerId = trainer.Id, IsDeleted = true };
            await _context.Client.AddAsync(activeClient);
            await _context.Client.AddAsync(deletedClient);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = activeClient.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                IsVisible = true,
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = deletedClient.Id,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                IsVisible = true,
                Confirmed = false
            });
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _paymentController.FilterClientPaymentsAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(1, response.Data);

            var remainingVisiblePayments = await _context.Payments.CountAsync(p => p.IsVisible);
            Assert.Equal(1, remainingVisiblePayments);
            Assert.True(await _context.Payments.AnyAsync(p => p.ClientId == activeClient.Id && p.IsVisible));
            Assert.True(await _context.Payments.IgnoreQueryFilters().AnyAsync(p => p.ClientId == deletedClient.Id && !p.IsVisible));
        }

        [Fact]
        public async Task TestFilterClientPaymentsReturnsZeroWhenNoOldPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [], TrainerId = trainer.Id, IsDeleted = false };
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
                Confirmed = true,
                IsVisible = true
            });
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _paymentController.FilterClientPaymentsAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data);
        }

        [Fact]
        public async Task TestFilterClientPaymentsReturnsNotFoundAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _paymentController.FilterClientPaymentsAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}
