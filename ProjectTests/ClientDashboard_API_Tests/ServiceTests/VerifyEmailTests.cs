using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class VerifyEmailTests
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
        private readonly IVerifyEmail _verifyEmail;

        public VerifyEmailTests()
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

            _verifyEmail = new VerifyEmail(_unitOfWork);
        }

        [Fact]
        public async Task TestHandleVerifiesEmailAndConsumesTokenForValidTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                EmailVerified = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();
            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var beforeHandle = DateTime.UtcNow.AddSeconds(-1);

            var success = await _verifyEmail.Handle(token.Id);

            Assert.True(success);

            var updatedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.True(updatedTrainer!.EmailVerified);

            var updatedToken = await _context.EmailVerificationToken.FindAsync(token.Id);
            Assert.True(updatedToken!.IsConsumed);
            Assert.True(updatedToken.ConsumedAt >= beforeHandle);
            Assert.True(updatedToken.ConsumedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task TestHandleReturnsFalseForNonExistentTokenAsync()
        {
            var success = await _verifyEmail.Handle(999);

            Assert.False(success);
        }

        [Fact]
        public async Task TestHandleReturnsFalseAndLeavesStateUnchangedForExpiredTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                EmailVerified = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();
            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1), // expired
                IsConsumed = false
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var success = await _verifyEmail.Handle(token.Id);

            Assert.False(success);

            var updatedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.False(updatedTrainer!.EmailVerified);

            var updatedToken = await _context.EmailVerificationToken.FindAsync(token.Id);
            Assert.False(updatedToken!.IsConsumed);
        }

        [Fact]
        public async Task TestHandleReturnsFalseWhenTrainerEmailAlreadyVerifiedAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                EmailVerified = true // already verified, e.g. via an earlier token
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();
            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var success = await _verifyEmail.Handle(token.Id);

            Assert.False(success);

            var updatedToken = await _context.EmailVerificationToken.FindAsync(token.Id);
            Assert.False(updatedToken!.IsConsumed);
        }
    }
}
