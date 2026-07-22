using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class PasswordResetTokenRepositoryTests
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

        public PasswordResetTokenRepositoryTests()
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
        }

        [Fact]
        public async Task TestAddPasswordResetTokenAsync()
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

            var rawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };

            await _passwordResetTokenRepository.AddPasswordResetTokenAsync(token);
            await _unitOfWork.Complete();

            var savedToken = await _context.PasswordResetToken.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(trainer.Id, savedToken.UserId);
            Assert.True(savedToken.CreatedOnUtc <= DateTime.UtcNow);
            Assert.True(savedToken.ExpiresOnUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByIdAsync()
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

            var rawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _passwordResetTokenRepository.GetPasswordResetTokenByIdAsync(token.Id);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
            Assert.Equal(trainer.Id, retrievedToken.UserId);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByIdReturnsNullForNonExistentIdAsync()
        {
            var token = await _passwordResetTokenRepository.GetPasswordResetTokenByIdAsync(999);

            Assert.Null(token);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashAsync()
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

            var rawToken = TokenGenerator.GenerateToken();
            var tokenHash = TokenGenerator.HashToken(rawToken);

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = tokenHash,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _passwordResetTokenRepository.GetPasswordResetTokenByTokenHashAsync(tokenHash);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashReturnsNullForUnknownHashAsync()
        {
            var retrievedToken = await _passwordResetTokenRepository.GetPasswordResetTokenByTokenHashAsync(TokenGenerator.HashToken(TokenGenerator.GenerateToken()));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestGetPasswordResetTokenByTokenHashDoesNotMatchADifferentRawTokenAsync()
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

            var storedRawToken = TokenGenerator.GenerateToken();

            var token = new PasswordResetToken
            {
                UserId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(storedRawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.PasswordResetToken.AddAsync(token);
            await _unitOfWork.Complete();

            var differentRawToken = TokenGenerator.GenerateToken();
            var retrievedToken = await _passwordResetTokenRepository.GetPasswordResetTokenByTokenHashAsync(TokenGenerator.HashToken(differentRawToken));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestValidateTokenAsyncReturnsTokenWhenValid()
        {
            var token = new PasswordResetToken
            {
                UserId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };

            var result = await _passwordResetTokenRepository.ValidateTokenAsync(token);

            Assert.NotNull(result);
            Assert.Equal(token, result);
        }

        [Fact]
        public async Task TestValidateTokenAsyncReturnsNullForExpiredToken()
        {
            var token = new PasswordResetToken
            {
                UserId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1),
                IsConsumed = false
            };

            var result = await _passwordResetTokenRepository.ValidateTokenAsync(token);

            Assert.Null(result);
        }

        [Fact]
        public async Task TestValidateTokenAsyncReturnsNullForConsumedToken()
        {
            var token = new PasswordResetToken
            {
                UserId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = true,
                ConsumedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            var result = await _passwordResetTokenRepository.ValidateTokenAsync(token);

            Assert.Null(result);
        }

        [Fact]
        public void TestConsumeTokenSetsIsConsumedAndConsumedAt()
        {
            var token = new PasswordResetToken
            {
                UserId = 1,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };

            var beforeConsume = DateTime.UtcNow.AddSeconds(-1);

            _passwordResetTokenRepository.ConsumeToken(token);

            Assert.True(token.IsConsumed);
            Assert.True(token.ConsumedAt >= beforeConsume);
            Assert.True(token.ConsumedAt <= DateTime.UtcNow.AddSeconds(1));
        }
    }
}
