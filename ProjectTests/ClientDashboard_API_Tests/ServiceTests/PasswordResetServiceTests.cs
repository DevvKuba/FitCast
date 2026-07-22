using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // Fake password reset link factory for testing
    public class FakePasswordResetLinkFactory : IPasswordResetLinkFactory
    {
        public List<string> CreatedForRawTokens { get; } = new();

        public string Create(string rawToken)
        {
            CreatedForRawTokens.Add(rawToken);
            return $"https://example.com/reset-password/{rawToken}";
        }
    }

    public class PasswordResetServiceTests
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
        private readonly FakePasswordResetLinkFactory _linkFactory;
        private readonly FakeFluentEmail _fluentEmail;
        private readonly PasswordResetService _passwordResetService;

        public PasswordResetServiceTests()
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

            _linkFactory = new FakePasswordResetLinkFactory();
            _fluentEmail = new FakeFluentEmail();
            _passwordResetService = new PasswordResetService(_unitOfWork, _linkFactory, _fluentEmail);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailCreatesTokenSuccessfullyForTrainerAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("OldPassword123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            Assert.Equal(trainer.Id, token.UserId);
            Assert.True(token.CreatedOnUtc >= beforeCreation);
            Assert.True(token.CreatedOnUtc <= DateTime.UtcNow.AddSeconds(1));
            Assert.Equal(token.CreatedOnUtc.AddDays(1), token.ExpiresOnUtc);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailCreatesTokenSuccessfullyForClientAsync()
        {
            // Arrange
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
                PasswordHash = _passwordHasher.Hash("ClientPassword123!")
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(client);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            Assert.Equal(client.Id, token.UserId);
            Assert.True(token.CreatedOnUtc >= beforeCreation);
            Assert.True(token.CreatedOnUtc <= DateTime.UtcNow.AddSeconds(1));
            Assert.Equal(token.CreatedOnUtc.AddDays(1), token.ExpiresOnUtc);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailSendsEmailToCorrectAddressAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "wilson",
                Email = "jane.wilson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentToEmails);
            Assert.Equal("jane.wilson@example.com", _fluentEmail.SentToEmails[0]);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailSendsCorrectSubjectAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "bob",
                Surname = "jones",
                Email = "bob.jones@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentSubjects);
            Assert.Equal("Password Reset for FitCast", _fluentEmail.SentSubjects[0]);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailIncludesResetLinkInBodyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "charlie",
                Surname = "brown",
                Email = "charlie.brown@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentBodies);
            var body = _fluentEmail.SentBodies[0];

            // Verify the body contains password reset link
            Assert.Contains("To reset your existing password", body);
            Assert.Contains("click here", body);
            Assert.Contains("https://example.com/reset-password/", body);

            // Verify the link contains the raw token, and that its hash matches what was persisted
            // (the raw token itself is never stored, so this is the only way to tie the two together)
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);
            Assert.Single(_linkFactory.CreatedForRawTokens);

            var rawToken = _linkFactory.CreatedForRawTokens[0];
            Assert.Contains($"https://example.com/reset-password/{rawToken}", body);
            Assert.Equal(TokenGenerator.HashToken(rawToken), tokens[0].TokenHash);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailSendsHtmlEmailAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "david",
                Surname = "miller",
                Email = "david.miller@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.BodyIsHtml);
            Assert.True(_fluentEmail.BodyIsHtml[0]); // Should be HTML format
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailSetsExpirationTo24HoursAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "eve",
                Surname = "garcia",
                Email = "eve.garcia@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var beforeCreation = DateTime.UtcNow;

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            var afterCreation = DateTime.UtcNow;

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            var expectedExpiry = token.CreatedOnUtc.AddDays(1);
            Assert.Equal(expectedExpiry, token.ExpiresOnUtc);

            // Verify expiration is approximately 24 hours from now
            var expiryDuration = token.ExpiresOnUtc - beforeCreation;
            Assert.True(expiryDuration.TotalHours >= 23.99); // Allow small timing variance
            Assert.True(expiryDuration.TotalHours <= 24.01);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailCreatesUniqueTokensForDifferentUsersAsync()
        {
            // Arrange
            var trainer1 = new Trainer
            {
                FirstName = "frank",
                Surname = "anderson",
                Email = "frank.anderson@example.com",
                PhoneNumber = "+1111111111",
                Role = UserRole.Trainer
            };
            var trainer2 = new Trainer
            {
                FirstName = "grace",
                Surname = "martinez",
                Email = "grace.martinez@example.com",
                PhoneNumber = "+2222222222",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddRangeAsync(trainer1, trainer2);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer1);
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer2);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.NotEqual(tokens[0].Id, tokens[1].Id); // Different token IDs
            Assert.Equal(trainer1.Id, tokens[0].UserId);
            Assert.Equal(trainer2.Id, tokens[1].UserId);

            // Verify different reset links were sent, each containing its own raw token
            Assert.Equal(2, _fluentEmail.SentBodies.Count);
            Assert.Equal(2, _linkFactory.CreatedForRawTokens.Count);
            Assert.NotEqual(_linkFactory.CreatedForRawTokens[0], _linkFactory.CreatedForRawTokens[1]);
            Assert.Contains($"https://example.com/reset-password/{_linkFactory.CreatedForRawTokens[0]}", _fluentEmail.SentBodies[0]);
            Assert.Contains($"https://example.com/reset-password/{_linkFactory.CreatedForRawTokens[1]}", _fluentEmail.SentBodies[1]);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailCanSendMultipleTimesToSameUserAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "henry",
                Surname = "davis",
                Email = "henry.davis@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act - Send password reset email twice (e.g., user clicked "resend")
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);
            await Task.Delay(100); // Small delay to ensure different timestamps
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Equal(2, tokens.Count); // Two separate tokens created
            Assert.All(tokens, token => Assert.Equal(trainer.Id, token.UserId));

            // Verify two emails were sent
            Assert.Equal(2, _fluentEmail.SentToEmails.Count);
            Assert.All(_fluentEmail.SentToEmails, email => Assert.Equal(trainer.Email, email));
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailUsesUtcTimeAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "iris",
                Surname = "rodriguez",
                Email = "iris.rodriguez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var utcBefore = DateTime.UtcNow;

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            var utcAfter = DateTime.UtcNow;

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            // Verify timestamps are in UTC range
            Assert.True(token.CreatedOnUtc >= utcBefore);
            Assert.True(token.CreatedOnUtc <= utcAfter);
            Assert.Equal(DateTimeKind.Utc, token.CreatedOnUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, token.ExpiresOnUtc.Kind);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailPersistsTokenToDatabaseAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jack",
                Surname = "wilson",
                Email = "jack.wilson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert - Verify token is persisted and retrievable
            var tokensFromDb = await _context.PasswordResetToken
                .Where(t => t.UserId == trainer.Id)
                .ToListAsync();

            Assert.Single(tokensFromDb);
            var token = tokensFromDb[0];
            Assert.True(token.Id > 0); // Should have auto-generated ID
            Assert.Equal(trainer.Id, token.UserId);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailWorksForBothTrainerAndClientAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "trainer",
                Surname = "user",
                Email = "trainer.user@example.com",
                PhoneNumber = "+9999999999",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "client",
                Email = "client.user@example.com",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(client);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Equal(2, tokens.Count);

            // Verify both users have tokens
            var trainerToken = tokens.FirstOrDefault(t => t.UserId == trainer.Id);
            var clientToken = tokens.FirstOrDefault(t => t.UserId == client.Id);

            Assert.NotNull(trainerToken);
            Assert.NotNull(clientToken);
            Assert.NotEqual(trainerToken.Id, clientToken.Id);

            // Verify both received emails
            Assert.Equal(2, _fluentEmail.SentToEmails.Count);
            Assert.Contains(trainer.Email, _fluentEmail.SentToEmails);
            Assert.Contains(client.Email, _fluentEmail.SentToEmails);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailGeneratesCorrectLinkFormatAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "kate",
                Surname = "hernandez",
                Email = "kate.hernandez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert
            var tokens = await _context.PasswordResetToken.ToListAsync();
            Assert.Single(tokens);
            Assert.Single(_linkFactory.CreatedForRawTokens);

            var expectedLink = $"https://example.com/reset-password/{_linkFactory.CreatedForRawTokens[0]}";

            Assert.Single(_fluentEmail.SentBodies);
            var body = _fluentEmail.SentBodies[0];
            Assert.Contains(expectedLink, body);
        }

        [Fact]
        public async Task TestCreateAndSendPasswordResetEmailDoesNotModifyUserPasswordAsync()
        {
            // Arrange
            var originalPassword = "OriginalPassword123!";
            var hashedPassword = _passwordHasher.Hash(originalPassword);

            var trainer = new Trainer
            {
                FirstName = "leo",
                Surname = "garcia",
                Email = "leo.garcia@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer,
                PasswordHash = hashedPassword
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _passwordResetService.CreateAndSendPasswordResetEmailAsync(trainer);

            // Assert - Password should remain unchanged
            var updatedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.NotNull(updatedTrainer);
            Assert.Equal(hashedPassword, updatedTrainer.PasswordHash);
            Assert.True(_passwordHasher.Verify(originalPassword, updatedTrainer.PasswordHash!));
        }
    }
}

