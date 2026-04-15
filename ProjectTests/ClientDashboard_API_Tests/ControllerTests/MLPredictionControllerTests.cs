using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Fake/Mock implementations for ML testing
    public class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ClientDashboard_API_Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    public class MLPredictionControllerTests : IDisposable
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
        private readonly TestWebHostEnvironment _webHostEnvironment;
        private readonly TrainerRevenueMLTrainingService _trainingService;
        private readonly TrainerRevenueMLPredictionService _predictionService;
        private readonly MLPredictionController _mlPredictionController;
        private readonly List<string> _tempDirectories = [];

        public MLPredictionControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            }, NullLoggerFactory.Instance);
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
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            // Setup temporary directory for ML models
            var tempRoot = Path.Combine(Path.GetTempPath(), $"ClientDashboard_MLTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);
            _tempDirectories.Add(tempRoot);

            _webHostEnvironment = new TestWebHostEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = tempRoot,
                WebRootPath = tempRoot,
                ContentRootFileProvider = new NullFileProvider(),
                WebRootFileProvider = new NullFileProvider(),
            };

            _trainingService = new TrainerRevenueMLTrainingService(
                _unitOfWork,
                NullLogger<TrainerRevenueMLTrainingService>.Instance,
                _webHostEnvironment);

            _predictionService = new TrainerRevenueMLPredictionService(
                _unitOfWork,
                NullLogger<TrainerRevenueMLPredictionService>.Instance,
                _webHostEnvironment);

            _mlPredictionController = new MLPredictionController(
                _predictionService,
                _trainingService,
                NullLogger<MLPredictionController>.Instance,
                _unitOfWork,
                _webHostEnvironment);
        }

        [Fact]
        public async Task TrainRevenueModelAsync_ReturnsOk_WhenTrainerHasSufficientDataAsync()
        {
            var trainerId = await AddTrainerAsync();
            await AddDummyRevenueDataAsync(trainerId, numberOfMonths: 6);

            var result = await _mlPredictionController.TrainRevenueModelAsync(trainerId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<ClientDashboard_API.ML.Models.ModelMetrics>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.True(response.Data.TrainingExamplesCount > 0);
            Assert.True(double.IsFinite(response.Data.RSquared));
            Assert.True(File.Exists(response.Data.ModelFilePath));
        }

        [Fact]
        public async Task TrainRevenueModelAsync_ReturnsBadRequest_WithInsufficientDataAsync()
        {
            var trainerId = await AddTrainerAsync();
            // No revenue data added

            var result = await _mlPredictionController.TrainRevenueModelAsync(trainerId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<ClientDashboard_API.ML.Models.ModelMetrics>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Insufficient data", response.Message);
        }

        [Fact]
        public async Task TrainModelAndPredictRevenueAsync_ReturnsBadRequest_WhenInsufficientDataAsync()
        {
            var trainerId = await AddTrainerAsync();

            var result = await _mlPredictionController.TrainModelAndPredictRevenueAsync(trainerId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<PredictionResultDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains("Need at least 2 months of data", response.Message);
        }

        [Fact]
        public async Task TrainModelAndPredictRevenueAsync_ReturnsPrediction_WhenTrainerHasSufficientDataAsync()
        {
            var trainerId = await AddTrainerAsync();
            await AddDummyRevenueDataAsync(trainerId, numberOfMonths: 6);

            var result = await _mlPredictionController.TrainModelAndPredictRevenueAsync(trainerId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<PredictionResultDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.True(response.Data.PredictedRevenue > 0);
            Assert.NotNull(response.Data.LowerBound);
            Assert.NotNull(response.Data.UpperBound);
            Assert.True(response.Data.LowerBound <= response.Data.PredictedRevenue);
            Assert.True(response.Data.UpperBound >= response.Data.PredictedRevenue);
            Assert.NotEqual("Insufficient", response.Data.Confidence);
        }

        [Fact]
        public async Task TrainModelAndPredictRevenueAsync_ReturnsNotFound_WhenTrainerDoesNotExistAsync()
        {
            var invalidTrainerId = 999;

            var result = await _mlPredictionController.TrainModelAndPredictRevenueAsync(invalidTrainerId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<PredictionResultDto>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GenerateDummyDataAsync_ReturnsOk_InDevelopmentEnvironmentAsync()
        {
            var trainerId = await AddTrainerAsync();

            var result = await _mlPredictionController.GenerateDummyDataAsync(trainerId, numberOfMonths: 3);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<DummyDataSummaryDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.True(response.Data.RecordsGenerated > 80); // 3 months ≈ 90 days

            var persistedCount = await _context.TrainerDailyRevenue.CountAsync(r => r.TrainerId == trainerId);
            Assert.Equal(response.Data.RecordsGenerated, persistedCount);
        }

        [Fact]
        public async Task GenerateDummyDataAsync_ReturnsBadRequest_WhenTrainerNotFoundAsync()
        {
            var invalidTrainerId = 999;

            var result = await _mlPredictionController.GenerateDummyDataAsync(invalidTrainerId, numberOfMonths: 3);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<DummyDataSummaryDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains($"Trainer {invalidTrainerId} not found", response.Message);
        }

        [Fact]
        public async Task GenerateDummyDataAsync_GeneratesConsistentRevenuePatternAsync()
        {
            var trainerId = await AddTrainerAsync();

            var result = await _mlPredictionController.GenerateDummyDataAsync(trainerId, numberOfMonths: 6);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<DummyDataSummaryDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.True(response.Data!.TotalRevenue > 0);
            Assert.True(response.Data.AverageMonthlyRevenue > 0);
            Assert.True(response.Data.EndingActiveClients >= response.Data.StartingActiveClients);
        }

        [Fact]
        public async Task GenerateDummyDataAsync_ReplacesExistingDataAsync()
        {
            var trainerId = await AddTrainerAsync();

            // Generate first batch
            await _mlPredictionController.GenerateDummyDataAsync(trainerId, numberOfMonths: 2);
            var countAfterFirst = await _context.TrainerDailyRevenue.CountAsync(r => r.TrainerId == trainerId);

            // Generate second batch (should replace)
            await _mlPredictionController.GenerateDummyDataAsync(trainerId, numberOfMonths: 4);
            var countAfterSecond = await _context.TrainerDailyRevenue.CountAsync(r => r.TrainerId == trainerId);

            Assert.True(countAfterSecond > countAfterFirst);
        }

        private async Task<int> AddTrainerAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = $"TestTrainer_{Guid.NewGuid():N}",
                Surname = "Unit",
                Email = $"trainer_{Guid.NewGuid():N}@test.local",
                DefaultCurrency = "GBP"
            };

            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer.Id;
        }

        private async Task AddDummyRevenueDataAsync(int trainerId, int numberOfMonths)
        {
            var records = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            foreach (var record in records)
            {
                await _unitOfWork.TrainerDailyRevenueRepository.AddTrainerDummyReveneRecordAsync(record);
            }

            await _unitOfWork.Complete();
        }

        public void Dispose()
        {
            foreach (var directory in _tempDirectories)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
                catch
                {
                    // Cleanup errors are non-critical
                }
            }
        }
    }
}
