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
    // Fake email verification link factory for testing
    public class FakeEmailVerificationLinkFactory : IEmailVerificationLinkFactory
    {
        public string Create(EmailVerificationToken emailVerificationToken)
        {
            return $"https://example.com/verify/{emailVerificationToken.Id}";
        }
    }

    // Fake FluentEmail for testing
    public class FakeFluentEmail : IFluentEmail
    {
        public List<string> SentToEmails { get; } = new();
        public List<string> SentSubjects { get; } = new();
        public List<string> SentBodies { get; } = new();
        public List<bool> BodyIsHtml { get; } = new();

        private string? _currentTo;
        private string? _currentSubject;
        private string? _currentBody;
        private bool _currentIsHtml;

        public IFluentEmail To(string emailAddress, string name = "")
        {
            _currentTo = emailAddress;
            return this;
        }

        public IFluentEmail Subject(string subject)
        {
            _currentSubject = subject;
            return this;
        }

        public IFluentEmail Body(string body, bool isHtml = false)
        {
            _currentBody = body;
            _currentIsHtml = isHtml;
            return this;
        }

        public Task<SendResponse> SendAsync(CancellationToken? token = null)
        {
            if (_currentTo != null)
            {
                SentToEmails.Add(_currentTo);
                SentSubjects.Add(_currentSubject ?? "");
                SentBodies.Add(_currentBody ?? "");
                BodyIsHtml.Add(_currentIsHtml);
            }

            return Task.FromResult(new SendResponse());
        }

        // Required interface properties/methods (not used in tests, minimal implementation)
        public EmailData Data { get; set; } = new EmailData();
        public ITemplateRenderer Renderer { get; set; } = null!;
        public ISender Sender { get; set; } = null!;

        // Unused interface methods (not needed for testing)
        public SendResponse Send(CancellationToken? token = null) => throw new NotImplementedException();
        public IFluentEmail SetFrom(string emailAddress, string name = "") => this;
        public IFluentEmail To(string emailAddress) => To(emailAddress, "");
        public IFluentEmail ReplyTo(string address, string name = "") => this;
        public IFluentEmail ReplyTo(string address) => this;
        public IFluentEmail CC(string emailAddress, string name = "") => this;
        public IFluentEmail CC(IEnumerable<Address> emailAddresses) => this;
        public IFluentEmail BCC(string emailAddress, string name = "") => this;
        public IFluentEmail BCC(IEnumerable<Address> emailAddresses) => this;
        public IFluentEmail To(IEnumerable<Address> emailAddresses) => this;
        public IFluentEmail AttachFromFilename(string filename, string contentType = null, string attachmentName = null) => this;
        public IFluentEmail Attach(Attachment attachment) => this;
        public IFluentEmail Attach(IEnumerable<Attachment> attachments) => this;
        public IFluentEmail PlaintextAlternativeBody(string body) => this;
        public IFluentEmail UsingTemplate<T>(string template, T model, bool isHtml = true) => this;
        public IFluentEmail UsingTemplateFromFile<T>(string filename, T model, bool isHtml = true) => this;
        public IFluentEmail UsingTemplateFromEmbedded<T>(string resourceName, T model, System.Reflection.Assembly assembly, bool isHtml = true) => this;
        public IFluentEmail UsingCultureTemplateFromFile<T>(string filename, T model, System.Globalization.CultureInfo culture, bool isHtml = true) => this;
        public IFluentEmail PlaintextAlternativeUsingTemplate<T>(string template, T model) => this;
        public IFluentEmail PlaintextAlternativeUsingTemplateFromFile<T>(string filename, T model) => this;
        public IFluentEmail PlaintextAlternativeUsingTemplateFromEmbedded<T>(string resourceName, T model, System.Reflection.Assembly assembly) => this;
        public IFluentEmail PlaintextAlternativeUsingCultureTemplateFromFile<T>(string filename, T model, System.Globalization.CultureInfo culture) => this;
        public IFluentEmail UsingTemplateEngine(ITemplateRenderer renderer) => this;
        public IFluentEmail Header(string header, string body) => this;
        public IFluentEmail Tag(string tag) => this;
        public IFluentEmail HighPriority() => this;
        public IFluentEmail LowPriority() => this;
    }

    public class EmailVerificationServiceTests
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
        private readonly FakeEmailVerificationLinkFactory _linkFactory;
        private readonly FakeFluentEmail _fluentEmail;
        private readonly EmailVerificationService _emailVerificationService;

        public EmailVerificationServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
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

            _linkFactory = new FakeEmailVerificationLinkFactory();
            _fluentEmail = new FakeFluentEmail();
            _emailVerificationService = new EmailVerificationService(_unitOfWork, _linkFactory, _fluentEmail);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailCreatesTokenSuccessfullyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var beforeCreation = DateTime.UtcNow.AddSeconds(-1); // Buffer for timing

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            var tokens = await _context.EmailVerificationToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            Assert.Equal(trainer.Id, token.TrainerId);
            Assert.True(token.CreatedOnUtc >= beforeCreation);
            Assert.True(token.CreatedOnUtc <= DateTime.UtcNow.AddSeconds(1));
            Assert.Equal(token.CreatedOnUtc.AddDays(1), token.ExpiresOnUtc);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailSendsEmailToCorrectAddressAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentToEmails);
            Assert.Equal("jane.smith@example.com", _fluentEmail.SentToEmails[0]);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailSendsCorrectSubjectAsync()
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
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentSubjects);
            Assert.Equal("Email verification for FitCast", _fluentEmail.SentSubjects[0]);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailIncludesVerificationLinkInBodyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "alice",
                Surname = "williams",
                Email = "alice.williams@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.SentBodies);
            var body = _fluentEmail.SentBodies[0];

            // Verify the body contains verification link
            Assert.Contains("To verify your email address", body);
            Assert.Contains("click here", body);
            Assert.Contains("https://example.com/verify/", body);

            // Verify the link includes the token ID
            var tokens = await _context.EmailVerificationToken.ToListAsync();
            Assert.Single(tokens);
            Assert.Contains($"https://example.com/verify/{tokens[0].Id}", body);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailSendsHtmlEmailAsync()
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
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            Assert.Single(_fluentEmail.BodyIsHtml);
            Assert.True(_fluentEmail.BodyIsHtml[0]); // Should be HTML format
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailSetsExpirationTo24HoursAsync()
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

            var beforeCreation = DateTime.UtcNow;

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            var afterCreation = DateTime.UtcNow;

            // Assert
            var tokens = await _context.EmailVerificationToken.ToListAsync();
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
        public async Task TestCreateAndSendVerificationEmailCreatesUniqueTokensForDifferentTrainersAsync()
        {
            // Arrange
            var trainer1 = new Trainer
            {
                FirstName = "eve",
                Surname = "davis",
                Email = "eve.davis@example.com",
                PhoneNumber = "+1111111111",
                Role = UserRole.Trainer
            };
            var trainer2 = new Trainer
            {
                FirstName = "frank",
                Surname = "garcia",
                Email = "frank.garcia@example.com",
                PhoneNumber = "+2222222222",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddRangeAsync(trainer1, trainer2);
            await _unitOfWork.Complete();

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer1);
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer2);

            // Assert
            var tokens = await _context.EmailVerificationToken.ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.NotEqual(tokens[0].Id, tokens[1].Id); // Different token IDs
            Assert.Equal(trainer1.Id, tokens[0].TrainerId);
            Assert.Equal(trainer2.Id, tokens[1].TrainerId);

            // Verify different verification links were sent
            Assert.Equal(2, _fluentEmail.SentBodies.Count);
            Assert.Contains($"https://example.com/verify/{tokens[0].Id}", _fluentEmail.SentBodies[0]);
            Assert.Contains($"https://example.com/verify/{tokens[1].Id}", _fluentEmail.SentBodies[1]);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailCanSendMultipleTimesToSameTrainerAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "grace",
                Surname = "martinez",
                Email = "grace.martinez@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act - Send verification email twice (e.g., resend scenario)
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);
            await Task.Delay(100); // Small delay to ensure different timestamps
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert
            var tokens = await _context.EmailVerificationToken.ToListAsync();
            Assert.Equal(2, tokens.Count); // Two separate tokens created
            Assert.All(tokens, token => Assert.Equal(trainer.Id, token.TrainerId));

            // Verify two emails were sent
            Assert.Equal(2, _fluentEmail.SentToEmails.Count);
            Assert.All(_fluentEmail.SentToEmails, email => Assert.Equal(trainer.Email, email));
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailUsesUtcTimeAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "henry",
                Surname = "wilson",
                Email = "henry.wilson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var utcBefore = DateTime.UtcNow;

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            var utcAfter = DateTime.UtcNow;

            // Assert
            var tokens = await _context.EmailVerificationToken.ToListAsync();
            Assert.Single(tokens);

            var token = tokens[0];
            // Verify timestamps are in UTC range
            Assert.True(token.CreatedOnUtc >= utcBefore);
            Assert.True(token.CreatedOnUtc <= utcAfter);
            Assert.Equal(DateTimeKind.Utc, token.CreatedOnUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, token.ExpiresOnUtc.Kind);
        }

        [Fact]
        public async Task TestCreateAndSendVerificationEmailPersistsTokenToDatabaseAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "iris",
                Surname = "anderson",
                Email = "iris.anderson@example.com",
                PhoneNumber = "+1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Act
            await _emailVerificationService.CreateAndSendVerificationEmailAsync(trainer);

            // Assert - Verify token is persisted and retrievable
            var tokensFromDb = await _context.EmailVerificationToken
                .Where(t => t.TrainerId == trainer.Id)
                .ToListAsync();

            Assert.Single(tokensFromDb);
            var token = tokensFromDb[0];
            Assert.True(token.Id > 0); // Should have auto-generated ID
            Assert.Equal(trainer.Id, token.TrainerId);
        }
    }
}
