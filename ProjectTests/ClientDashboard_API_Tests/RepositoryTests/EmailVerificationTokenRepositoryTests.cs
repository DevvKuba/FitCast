using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
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
    // covers the shared ITokenRepository<EmailVerificationToken> surface (inherited from
    // TokenRepository<EmailVerificationToken>) plus the entity-specific
    // GetTokenByIdWithTrainerAsync. IsValid()/Consume() now live on TokenBase and are
    // covered once, generically, in TokenBaseTests.cs rather than duplicated per repo.
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
        public async Task TestAddEmailVerificationTokenAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };

            await _emailVerificationTokenRepository.AddTokenAsync(token);
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
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var rawToken = TokenGenerator.GenerateToken();

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _emailVerificationTokenRepository.GetTokenByIdAsync(token.Id);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
            Assert.Equal(trainer.Id, retrievedToken.TrainerId);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByIdReturnsNullForNonExistentIdAsync()
        {
            var token = await _emailVerificationTokenRepository.GetTokenByIdAsync(999);

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
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var createdTime = DateTime.UtcNow;
            var expirationTime = createdTime.AddHours(48);
            var rawToken = TokenGenerator.GenerateToken();

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = createdTime,
                ExpiresOnUtc = expirationTime
            };

            await _emailVerificationTokenRepository.AddTokenAsync(token);
            await _unitOfWork.Complete();

            var savedToken = await _context.EmailVerificationToken.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(trainer.Id, savedToken.TrainerId);
            Assert.Equal(createdTime.Date, savedToken.CreatedOnUtc.Date);
            Assert.Equal(expirationTime.Date, savedToken.ExpiresOnUtc.Date);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByTokenHashAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = tokenHash,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var retrievedToken = await _emailVerificationTokenRepository.GetTokenByTokenHashAsync(tokenHash);

            Assert.NotNull(retrievedToken);
            Assert.Equal(token.Id, retrievedToken.Id);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByTokenHashReturnsNullForUnknownHashAsync()
        {
            var retrievedToken = await _emailVerificationTokenRepository.GetTokenByTokenHashAsync(TokenGenerator.HashToken(TokenGenerator.GenerateToken()));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByTokenHashDoesNotMatchADifferentRawTokenAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(storedRawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var differentRawToken = TokenGenerator.GenerateToken();
            var retrievedToken = await _emailVerificationTokenRepository.GetTokenByTokenHashAsync(TokenGenerator.HashToken(differentRawToken));

            Assert.Null(retrievedToken);
        }

        [Fact]
        public async Task TestGetEmailVerificationTokenByIdWithTrainerIncludesTrainerAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            // clear the tracked context so the navigation property can only be populated via .Include
            _context.ChangeTracker.Clear();

            var retrievedToken = await _emailVerificationTokenRepository.GetTokenByIdWithTrainerAsync(token.Id);

            Assert.NotNull(retrievedToken);
            Assert.NotNull(retrievedToken.Trainer);
            Assert.Equal(trainer.Id, retrievedToken.Trainer!.Id);
        }

        [Fact]
        public async Task TestRemoveTokenAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            _emailVerificationTokenRepository.RemoveToken(token);
            await _unitOfWork.Complete();

            var remainingToken = await _context.EmailVerificationToken.FindAsync(token.Id);
            Assert.Null(remainingToken);
        }

        [Fact]
        public async Task TestGetAllExpiredOrConsumedTokensReturnsOnlyInvalidTokensAsync()
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

            var validToken = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = false
            };
            var expiredToken = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1),
                IsConsumed = false
            };
            var consumedToken = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                TokenHash = TokenGenerator.HashToken(TokenGenerator.GenerateToken()),
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24),
                IsConsumed = true,
                ConsumedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            await _context.EmailVerificationToken.AddRangeAsync(validToken, expiredToken, consumedToken);
            await _unitOfWork.Complete();

            var invalidTokens = await _emailVerificationTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            Assert.Equal(2, invalidTokens.Count);
            Assert.Contains(invalidTokens, t => t.Id == expiredToken.Id);
            Assert.Contains(invalidTokens, t => t.Id == consumedToken.Id);
            Assert.DoesNotContain(invalidTokens, t => t.Id == validToken.Id);
        }
    }
}
