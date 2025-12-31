using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class EmailVerificationTokenRepositoryTests
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

        public EmailVerificationTokenRepositoryTests()
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
        public async Task TestAddEmailVerificationTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = "trainer"
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };

            await _emailVerificationTokenRepository.AddEmailVerificationTokenAsync(token);
            await _unitOfWork.Complete();

            var savedToken = await _context.EmailVerificationToken.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(trainer.Id, savedToken.TrainerId);
            Assert.True(savedToken.CreatedOnUtc <= DateTime.UtcNow);
            Assert.True(savedToken.ExpiresOnUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByIdAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = "trainer"
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _emailVerificationTokenRepository.GetEmailVerificationTokenByIdAsync(token.Id);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
            Assert.Equal(trainer.Id, retrievedToken.TrainerId);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByIdReturnsNullForNonExistentIdAsync()
        {
            var token = await _emailVerificationTokenRepository.GetEmailVerificationTokenByIdAsync(999);

            Assert.Null(token);
        }

        [Fact]
        public async Task TestAddEmailVerificationTokenWithExpirationAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = "trainer"
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var createdTime = DateTime.UtcNow;
            var expirationTime = createdTime.AddHours(48);

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                CreatedOnUtc = createdTime,
                ExpiresOnUtc = expirationTime
            };

            await _emailVerificationTokenRepository.AddEmailVerificationTokenAsync(token);
            await _unitOfWork.Complete();

            var savedToken = await _context.EmailVerificationToken.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(trainer.Id, savedToken.TrainerId);
            Assert.Equal(createdTime.Date, savedToken.CreatedOnUtc.Date);
            Assert.Equal(expirationTime.Date, savedToken.ExpiresOnUtc.Date);
        }
    }
}
