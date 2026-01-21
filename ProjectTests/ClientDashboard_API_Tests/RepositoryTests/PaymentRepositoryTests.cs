using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class PaymentRepositoryTests
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

        public PaymentRepositoryTests()
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
        public async Task TestGetAllPaymentsForTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var payments = await _paymentRepository.GetAllPaymentsForTrainerAsync(trainer);

            Assert.Equal(2, payments.Count);
            Assert.False(payments[0].Confirmed);
            Assert.True(payments[1].Confirmed);
        }

        [Fact]
        public async Task TestGetAllClientSpecificPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
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

            var payments = await _paymentRepository.GetAllClientSpecificPaymentsAsync(client);

            Assert.Equal(2, payments.Count);
            Assert.All(payments, p => Assert.Equal(client.Id, p.ClientId));
            Assert.False(payments[0].Confirmed);
            Assert.True(payments[1].Confirmed);
        }

        [Fact]
        public async Task TestGetPaymentByIdAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = trainer.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            var retrievedPayment = await _paymentRepository.GetPaymentByIdAsync(payment.Id);

            Assert.NotNull(retrievedPayment);
            Assert.Equal(payment.Id, retrievedPayment.Id);
            Assert.Equal(100.00m, retrievedPayment.Amount);
        }

        [Fact]
        public async Task TestGetPaymentByIdReturnsNullForNonExistentIdAsync()
        {
            var payment = await _paymentRepository.GetPaymentByIdAsync(999);

            Assert.Null(payment);
        }

        [Fact]
        public async Task TestGetPaymentWithClientByIdAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
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

            var retrievedPayment = await _paymentRepository.GetPaymentWithClientByIdAsync(payment.Id);

            Assert.NotNull(retrievedPayment);
            Assert.NotNull(retrievedPayment.Client);
            Assert.Equal(client.Id, retrievedPayment.Client.Id);
            Assert.Equal("rob", retrievedPayment.Client.FirstName);
        }

        [Fact]
        public async Task TestGetPaymentWithRelatedEntitiesByIdAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
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

            var retrievedPayment = await _paymentRepository.GetPaymentWithRelatedEntitiesById(payment.Id);

            Assert.NotNull(retrievedPayment);
            Assert.NotNull(retrievedPayment.Client);
            Assert.NotNull(retrievedPayment.Trainer);
            Assert.Equal(client.Id, retrievedPayment.Client.Id);
            Assert.Equal(trainer.Id, retrievedPayment.Trainer.Id);
        }

        [Fact]
        public async Task TestUpdatePaymentDetailsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = trainer.Id,
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

            _paymentRepository.UpdatePaymentDetails(payment, updateDto);
            await _unitOfWork.Complete();

            Assert.Equal(200.00m, payment.Amount);
            Assert.Equal("$", payment.Currency);
            Assert.Equal(12, payment.NumberOfSessions);
            Assert.Equal(DateOnly.Parse("20/06/2024"), payment.PaymentDate);
            Assert.True(payment.Confirmed);
        }

        [Fact]
        public async Task TestCalculateClientTotalLifetimeValueAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
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
                Confirmed = true
            });
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Amount = 50.00m,
                Currency = "£",
                NumberOfSessions = 4,
                PaymentDate = DateOnly.Parse("25/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var totalValue = await _paymentRepository.CalculateClientTotalLifetimeValueAsync(client, DateOnly.Parse("30/06/2024"));

            Assert.Equal(250.00m, totalValue);
        }

        [Fact]
        public async Task TestCalculateClientTotalLifetimeValueReturnsZeroWhenNoConfirmedPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
            await _unitOfWork.Complete();

            var totalValue = await _paymentRepository.CalculateClientTotalLifetimeValueAsync(client, DateOnly.Parse("30/06/2024"));

            Assert.Equal(0m, totalValue);
        }

        [Fact]
        public async Task TestAddNewPaymentAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "£" };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
            await _unitOfWork.Complete();

            await _paymentRepository.AddNewPaymentAsync(trainer, client, 8, 120.00m, DateOnly.Parse("15/06/2024"), true);
            await _unitOfWork.Complete();

            var payment = await _context.Payments.FirstOrDefaultAsync();

            Assert.NotNull(payment);
            Assert.Equal(trainer.Id, payment.TrainerId);
            Assert.Equal(client.Id, payment.ClientId);
            Assert.Equal(8, payment.NumberOfSessions);
            Assert.Equal(120.00m, payment.Amount);
            Assert.Equal("£", payment.Currency);
            Assert.True(payment.Confirmed);
        }

        [Fact]
        public async Task TestAddNewPaymentWithNullConfirmedDefaultsToFalseAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "$" };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
            await _unitOfWork.Complete();

            await _paymentRepository.AddNewPaymentAsync(trainer, client, 8, 120.00m, DateOnly.Parse("15/06/2024"), null);
            await _unitOfWork.Complete();

            var payment = await _context.Payments.FirstOrDefaultAsync();

            Assert.NotNull(payment);
            Assert.False(payment.Confirmed);
        }

        [Fact]
        public async Task TestAddNewPaymentUsesDefaultCurrencyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, DefaultCurrency = "€" };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
            await _unitOfWork.Complete();

            await _paymentRepository.AddNewPaymentAsync(trainer, client, 8, 120.00m, DateOnly.Parse("15/06/2024"), true);
            await _unitOfWork.Complete();

            var payment = await _context.Payments.FirstOrDefaultAsync();

            Assert.NotNull(payment);
            Assert.Equal("€", payment.Currency);
        }

        [Fact]
        public async Task TestFilterOldClientPaymentsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
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

            var removedCount = await _paymentRepository.FilterOldClientPaymentsAsync(trainer);
            await _unitOfWork.Complete();

            Assert.Equal(2, removedCount);
            Assert.False(_context.Payments.Any(p => p.ClientId == null && p.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task TestFilterOldClientPaymentsDoesNotRemovePaymentsWithClientsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.AddAsync(trainer);
            await _context.AddAsync(client);
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
                ClientId = null,
                Amount = 150.00m,
                Currency = "£",
                NumberOfSessions = 12,
                PaymentDate = DateOnly.Parse("20/06/2024"),
                Confirmed = false
            });
            await _unitOfWork.Complete();

            var removedCount = await _paymentRepository.FilterOldClientPaymentsAsync(trainer);
            await _unitOfWork.Complete();

            Assert.Equal(1, removedCount);
            Assert.True(_context.Payments.Any(p => p.ClientId == client.Id));
            Assert.False(_context.Payments.Any(p => p.ClientId == null));
        }

        [Fact]
        public async Task TestDeletePaymentAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            var payment = new Payment
            {
                TrainerId = trainer.Id,
                Amount = 100.00m,
                Currency = "£",
                NumberOfSessions = 8,
                PaymentDate = DateOnly.Parse("15/06/2024"),
                Confirmed = true
            };
            await _context.Payments.AddAsync(payment);
            await _unitOfWork.Complete();

            _paymentRepository.DeletePayment(payment);
            await _unitOfWork.Complete();

            Assert.False(_context.Payments.Any());
        }
    }
}
