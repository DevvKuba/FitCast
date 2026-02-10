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
    // Fake implementations for testing
    public class FakeRegisterService : IRegisterService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public FakeRegisterService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponseDto<string>> Handle(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new ApiResponseDto<string> { Data = null, Message = "Email already registered", Success = false };
            }

            // Simulate successful registration
            return new ApiResponseDto<string> { Data = registerDto.FirstName, Message = "Registration successful", Success = true };
        }

        public async Task<bool> MapClientDataUponRegistrationAsync(RegisterDto registerDto)
        {
            // Simulate mapping client data to trainer
            if (registerDto.ClientId == null || registerDto.ClientsTrainerId == null)
            {
                return false;
            }

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(registerDto.ClientsTrainerId.Value);
            var client = await _unitOfWork.ClientRepository.GetClientByIdAsync(registerDto.ClientId.Value);

            if (trainer == null || client == null)
            {
                return false;
            }

            // Simulate updating client details with registration data
            client.Email = registerDto.Email;
            client.PhoneNumber = registerDto.PhoneNumber;
            client.PasswordHash = _passwordHasher.Hash(registerDto.Password);
            client.TrainerId = trainer.Id;

            await _unitOfWork.Complete();
            return true;
        }
    }

    public class FakeLoginService : ILoginService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenProvider _tokenProvider;

        public FakeLoginService(IUnitOfWork unitOfWork, ITokenProvider tokenProvider)
        {
            _unitOfWork = unitOfWork;
            _tokenProvider = tokenProvider;
        }

        public async Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto)
        {
            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "User not found", Success = false };
            }

            // For testing, accept password "correctPassword"
            if (loginDto.Password != "correctPassword")
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "Invalid password", Success = false };
            }

            var token = _tokenProvider.Create(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                Token = token,
                Role = user.Role
            };

            return new ApiResponseDto<UserDto> { Data = userDto, Message = "Login successful", Success = true };
        }
    }

    public class FakeTokenProvider : ITokenProvider
    {
        public string Create(UserBase user)
        {
            // Return a fake token for testing
            return $"fake_token_{user.Id}_{user.Email}";
        }
    }

    public class FakeEmailVerificationService : IEmailVerificationService
    {
        public Task CreateAndSendVerificationEmailAsync(Trainer trainer)
        {
            // Simulate sending email without actual email logic
            return Task.CompletedTask;
        }
    }

    public class FakePasswordResetService : IPasswordResetService
    {
        public Task CreateAndSendPasswordResetEmailAsync(UserBase user)
        {
            // Simulate sending email without actual email logic
            return Task.CompletedTask;
        }
    }

    public class FakeVerifyEmail : IVerifyEmail
    {
        private readonly IUnitOfWork _unitOfWork;

        public FakeVerifyEmail(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(int tokenId)
        {
            var token = await _unitOfWork.EmailVerificationTokenRepository.GetEmailVerificationTokenByIdAsync(tokenId);

            if (token == null || token.ExpiresOnUtc < DateTime.UtcNow)
            {
                return false;
            }

            var trainer = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(token.TrainerId);
            if (trainer != null)
            {
                trainer.EmailVerified = true;
                await _unitOfWork.Complete();
            }

            return true;
        }
    }

    public class AccountControllerTests
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
        private readonly IRegisterService _fakeRegisterService;
        private readonly ILoginService _fakeLoginService;
        private readonly IEmailVerificationService _fakeEmailVerificationService;
        private readonly IPasswordResetService _fakePasswordResetService;
        private readonly IVerifyEmail _fakeVerifyEmail;
        private readonly ITokenProvider _fakeTokenProvider;
        private readonly AccountController _accountController;

        public AccountControllerTests()
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

            _fakeTokenProvider = new FakeTokenProvider();
            _fakeRegisterService = new FakeRegisterService(_unitOfWork, _passwordHasher);
            _fakeLoginService = new FakeLoginService(_unitOfWork, _fakeTokenProvider);
            _fakeEmailVerificationService = new FakeEmailVerificationService();
            _fakePasswordResetService = new FakePasswordResetService();
            _fakeVerifyEmail = new FakeVerifyEmail(_unitOfWork);
            _accountController = new AccountController(_unitOfWork, _fakeRegisterService, _fakeLoginService, _fakeEmailVerificationService, _fakePasswordResetService, _passwordHasher, _fakeVerifyEmail);
        }

        [Fact]
        public async Task TestRegisterCreatesNewUserSuccessfullyAsync()
        {
            var registerDto = new RegisterDto
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                PhoneNumber = "1234567890",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = UserRole.Trainer
            };

            var result = await _accountController.Register(registerDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);
        }

        [Fact]
        public async Task TestRegisterReturnsBadRequestForDuplicateEmailAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Email = "john@example.com", 
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("Password123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var registerDto = new RegisterDto
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "john@example.com",
                PhoneNumber = "0987654321",
                Password = "Password456!",
                ConfirmPassword = "Password456!",
                Role = UserRole.Trainer
            };

            var result = await _accountController.Register(registerDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestLoginReturnsTokenForValidCredentialsAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("correctPassword")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "john@example.com",
                Password = "correctPassword",
                Role = UserRole.Trainer
            };

            var result = await _accountController.Login(loginDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<UserDto>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("john", response.Data!.FirstName);
            Assert.NotNull(response.Data.Token);
        }

        [Fact]
        public async Task TestLoginReturnsNotFoundForNonExistentUserAsync()
        {
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!",
                Role = UserRole.Trainer
            };

            var result = await _accountController.Login(loginDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<UserDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestLoginReturnsNotFoundForInvalidPasswordAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("correctPassword")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var loginDto = new LoginDto
            {
                Email = "john@example.com",
                Password = "wrongPassword",
                Role = UserRole.Trainer
            };

            var result = await _accountController.Login(loginDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<UserDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestVerifyEmailVerificationTokenSucceedsAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(24)
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var result = await _accountController.VerifyEmailVerificationTokenAsync(token.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var updatedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.True(updatedTrainer!.EmailVerified);
        }

        [Fact]
        public async Task TestVerifyEmailVerificationTokenReturnsNotFoundForInvalidTokenAsync()
        {
            var result = await _accountController.VerifyEmailVerificationTokenAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestVerifyEmailVerificationTokenReturnsBadRequestForExpiredTokenAsync()
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

            var token = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1) // Expired
            };
            await _context.EmailVerificationToken.AddAsync(token);
            await _unitOfWork.Complete();

            var result = await _accountController.VerifyEmailVerificationTokenAsync(token.Id);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestVerifyClientsTrainerStatusReturnsClientInfoSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                PhoneNumber = "1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 1,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _accountController.VerfiyClientsTrainerStatusAsync("1234567890", "alice");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(client.Id, response.Data!.ClientId);
            Assert.Equal(trainer.Id, response.Data.TrainerId);
        }

        [Fact]
        public async Task TestVerifyClientsTrainerStatusReturnsNotFoundForNonExistentTrainerAsync()
        {
            var result = await _accountController.VerfiyClientsTrainerStatusAsync("9999999999", "alice");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestVerifyClientsTrainerStatusReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                PhoneNumber = "1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _accountController.VerfiyClientsTrainerStatusAsync("1234567890", "nonexistent");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestResendEmailVerificationSendsEmailSuccessfullyAsync()
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

            var result = await _accountController.ResendEmailVerificationForTrainerAsync("john@example.com");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);
        }

        [Fact]
        public async Task TestResendEmailVerificationReturnsNotFoundForNonExistentTrainerAsync()
        {
            var result = await _accountController.ResendEmailVerificationForTrainerAsync("nonexistent@example.com");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestResendEmailVerificationReturnsBadRequestForAlreadyVerifiedEmailAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                EmailVerified = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _accountController.ResendEmailVerificationForTrainerAsync("john@example.com");
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestSendPasswordResetEmailSendsEmailSuccessfullyAsync()
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

            var result = await _accountController.SendPasswordResetEmailForUserAsync("john@example.com");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);
        }

        [Fact]
        public async Task TestSendPasswordResetEmailReturnsNotFoundForNonExistentUserAsync()
        {
            var result = await _accountController.SendPasswordResetEmailForUserAsync("nonexistent@example.com");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<ClientVerificationInfoDto>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestChangeUserPasswordChangesPasswordSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("OldPassword123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var resetToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(1),
                IsConsumed = false
            };
            await _context.PasswordResetToken.AddAsync(resetToken);
            await _unitOfWork.Complete();

            var passwordResetDto = new PasswordResetDto
            {
                TokenId = resetToken.Id,
                NewPassword = "NewPassword456!"
            };

            var result = await _accountController.ChangeUserPasswordAsync(passwordResetDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var updatedToken = await _context.PasswordResetToken.FindAsync(resetToken.Id);
            Assert.True(updatedToken!.IsConsumed);
        }

        [Fact]
        public async Task TestChangeUserPasswordReturnsNotFoundForInvalidUserAsync()
        {
            var passwordResetDto = new PasswordResetDto
            {
                TokenId = 999,
                NewPassword = "NewPassword456!"
            };

            var result = await _accountController.ChangeUserPasswordAsync(passwordResetDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestChangeUserPasswordReturnsBadRequestForSamePasswordAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("SamePassword123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var resetToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(1),
                IsConsumed = false
            };
            await _context.PasswordResetToken.AddAsync(resetToken);
            await _unitOfWork.Complete();

            var passwordResetDto = new PasswordResetDto
            {
                TokenId = resetToken.Id,
                NewPassword = "SamePassword123!"
            };

            var result = await _accountController.ChangeUserPasswordAsync(passwordResetDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestChangeUserPasswordReturnsBadRequestForConsumedTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("OldPassword123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var resetToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(1),
                IsConsumed = true,
                ConsumedAt = DateTime.UtcNow.AddMinutes(-30)
            };
            await _context.PasswordResetToken.AddAsync(resetToken);
            await _unitOfWork.Complete();

            var passwordResetDto = new PasswordResetDto
            {
                TokenId = resetToken.Id,
                NewPassword = "NewPassword456!"
            };

            var result = await _accountController.ChangeUserPasswordAsync(passwordResetDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestChangeUserPasswordReturnsBadRequestForExpiredTokenAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer,
                PasswordHash = _passwordHasher.Hash("OldPassword123!")
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var resetToken = new PasswordResetToken
            {
                UserId = trainer.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(-1), // Expired
                IsConsumed = false
            };
            await _context.PasswordResetToken.AddAsync(resetToken);
            await _unitOfWork.Complete();

            var passwordResetDto = new PasswordResetDto
            {
                TokenId = resetToken.Id,
                NewPassword = "NewPassword456!"
            };

            var result = await _accountController.ChangeUserPasswordAsync(passwordResetDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}
