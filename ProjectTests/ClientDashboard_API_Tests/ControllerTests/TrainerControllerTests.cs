using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Fake implementations for testing
    public class FakeTrainerFullMonthAnalyticsService : ITrainerFullMonthAnalyticsService
    {
        public ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            return new ClientMetricsDto();
        }

        public RevenuePatternsDto GetRevenuePatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            return new RevenuePatternsDto();
        }

        public ActivityPatternsDto GetActivityPatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            return new ActivityPatternsDto
            {
                AllWeekdays = [],
                BusiestDays = [],
                LightDays = []
            };
        }

        public CompleteMonthTrainerAnalyticsDto GetAllAnalyticMetrics(List<TrainerDailyRevenue> allRevenueRecords)
        {
            return new CompleteMonthTrainerAnalyticsDto
            {
                AllWeekdays = [],
                BusiestDays = [],
                LightDays = []
            };
        }

        public WorktimeMetricsDto GetWorktimeMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            return new WorktimeMetricsDto();
        }
    }

    public class FakeTrainerCurrentMonthAnalyticsService : ITrainerCurrentMonthAnalyticsService
    {
        public CurrentMonthTrainerAnalyticsDto GetCurrentMonthsAnalyticMetrics(List<TrainerDailyRevenue> currentRevenueRecords)
        {
            return new CurrentMonthTrainerAnalyticsDto();
        }
    }

    public class TrainerControllerTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly FakeTrainerFullMonthAnalyticsService _fakeTrainerFullMonthAnalyticsService;
        private readonly FakeTrainerCurrentMonthAnalyticsService _fakeTrainerCurrentMonthAnalyticsService;
        private readonly TrainerDependencyRemovalHelper _trainerDependencyRemovalHelper;
        private readonly TrainerController _trainerController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public TrainerControllerTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _mapper = testUnitOfWork.mapper;
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            _fakeTrainerFullMonthAnalyticsService = new FakeTrainerFullMonthAnalyticsService();
            _fakeTrainerCurrentMonthAnalyticsService = new FakeTrainerCurrentMonthAnalyticsService();

            var (_, currentUserAccessor, httpContextAccessor) = TestAuthHelpers.CreateAuthInfrastructure();
            _httpContextAccessor = httpContextAccessor;

            _trainerDependencyRemovalHelper = new TrainerDependencyRemovalHelper(_unitOfWork);

            _trainerController = new TrainerController(_unitOfWork, _mapper, _fakeTrainerFullMonthAnalyticsService, _fakeTrainerCurrentMonthAnalyticsService, currentUserAccessor, _trainerDependencyRemovalHelper);
            TestAuthHelpers.AttachHttpContext(_trainerController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);

        private async Task<(Trainer Trainer, Client Client)> SeedTrainerWithClientAsync(string timeZoneId = SupportedTimeZones.London)
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, TimeZoneId = timeZoneId };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            return (trainer, client);
        }

        // A 60 minute Scheduled slot 30 days ahead, starting at the given local clock time in the given zone
        private async Task<BookedSessionSlot> SeedFutureSlotAsync(Trainer trainer, Client client, string timeZoneId, TimeOnly localStart)
        {
            var localDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(localStart), TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

            var slot = new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "session",
                StartDateTime = startUtc,
                EndDateTime = startUtc.AddMinutes(60),
                Status = SessionStatus.Scheduled
            };
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();
            return slot;
        }

        private static TimeOnly LocalStartTime(BookedSessionSlot slot, string timeZoneId) =>
            TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(slot.StartDateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)));

        [Fact]
        public async Task TestRetrieveTrainerByIdReturnsTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.RetrieveTrainerByIdAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data!.FirstName);
        }

        [Fact]
        public async Task TestRetrieveTrainerByIdReturnsNotFoundAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _trainerController.RetrieveTrainerByIdAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Email = "john@example.com", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var updateDto = new TrainerUpdateDto
            {
                FirstName = "jonathan",
                Surname = "doe jr",
                Email = "jonathan@example.com",
                PhoneNumber = "1234567890",
                BusinessName = "New Business",
                DefaultCurrency = "�",
                AverageSessionPrice = 50.00m,
                TimeZoneId = SupportedTimeZones.London,
                CalendarSyncEnabled = true
            };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateTrainerProfileAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("jonathan", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("jonathan", savedTrainer!.FirstName);
            Assert.Equal("doe jr", savedTrainer.Surname);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileReturnsNotFoundAsync()
        {
            var updateDto = new TrainerUpdateDto
            {
                FirstName = "jonathan",
                Surname = "doe",
                Email = "jonathan@example.com",
                TimeZoneId = SupportedTimeZones.London,
                CalendarSyncEnabled = true
            };

            AuthenticateAsTrainer(999);
            var result = await _trainerController.UpdateTrainerProfileAsync(updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileReturnsBadRequestForUnsupportedTimeZoneAsync()
        {
            var (trainer, _) = await SeedTrainerWithClientAsync();

            var updateDto = new TrainerUpdateDto { FirstName = "jonathan", TimeZoneId = "Mars/Olympus_Mons", CalendarSyncEnabled = true };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateTrainerProfileAsync(updateDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("john", savedTrainer!.FirstName);
            Assert.Equal(SupportedTimeZones.London, savedTrainer.TimeZoneId);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileShiftsFutureSlotsToKeepClockTimeInNewTimeZoneAsync()
        {
            var (trainer, client) = await SeedTrainerWithClientAsync(SupportedTimeZones.London);
            var slot = await SeedFutureSlotAsync(trainer, client, SupportedTimeZones.London, new TimeOnly(9, 0));
            var originalStartUtc = slot.StartDateTime;

            var updateDto = new TrainerUpdateDto { FirstName = "john", TimeZoneId = SupportedTimeZones.NewYork, CalendarSyncEnabled = true };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateTrainerProfileAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal(SupportedTimeZones.NewYork, savedTrainer!.TimeZoneId);

            var savedSlot = await _context.BookedSessionsSlot.FindAsync(slot.Id);
            Assert.NotEqual(originalStartUtc, savedSlot!.StartDateTime);
            Assert.Equal(new TimeOnly(9, 0), LocalStartTime(savedSlot, SupportedTimeZones.NewYork));
            Assert.Equal(TimeSpan.FromMinutes(60), savedSlot.EndDateTime - savedSlot.StartDateTime);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileLeavesSlotsUntouchedWhenTimeZoneUnchangedAsync()
        {
            var (trainer, client) = await SeedTrainerWithClientAsync(SupportedTimeZones.London);
            var slot = await SeedFutureSlotAsync(trainer, client, SupportedTimeZones.London, new TimeOnly(9, 0));
            var originalStartUtc = slot.StartDateTime;

            var updateDto = new TrainerUpdateDto { FirstName = "jonathan", TimeZoneId = SupportedTimeZones.London, CalendarSyncEnabled = true };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateTrainerProfileAsync(updateDto);
            var okResult = result.Result as OkObjectResult;

            Assert.NotNull(okResult);

            var savedSlot = await _context.BookedSessionsSlot.FindAsync(slot.Id);
            Assert.Equal(originalStartUtc, savedSlot!.StartDateTime);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateClientAssignmentAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(trainer.Id, savedClient!.TrainerId);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(999);
            var result = await _trainerController.UpdateClientAssignmentAsync(client.Id);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateClientAssignmentAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateTrainerPaymentSettingSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoPaymentSetting = false, AverageSessionPrice = 40, DefaultCurrency = "�" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.UpdateTrainerPaymentSettingAsync(true);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.True(savedTrainer!.AutoPaymentSetting);
        }

        [Fact]
        public async Task TestUpdateTrainerPaymentSettingReturnsNotFoundAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _trainerController.UpdateTrainerPaymentSettingAsync(true);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetAutoPaymentSettingStatusSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoPaymentSetting = true };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.GetAutoPaymentSettingStatusAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task TestGetAutoPaymentSettingStatusReturnsNotFoundAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _trainerController.GetAutoPaymentSettingStatusAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestDeleteTrainerReturnsNotFoundForUnknownTrainerAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _trainerController.DeleteTrainerAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.False(response.Data);
        }

        [Fact]
        public async Task TestDeleteTrainerRemovesTrainerAndAllDependenciesAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Notification.AddAsync(new Notification { TrainerId = trainer.Id, ClientId = client.Id, Message = "n", ReminderType = NotificationType.TrainerBlockCompletionReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow });
            await _context.Payments.AddAsync(new Payment { TrainerId = trainer.Id, ClientId = client.Id, Amount = 120, Currency = "GBP", NumberOfSessions = 4, PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow) });
            await _unitOfWork.Complete();
            _unitOfWork.Clear();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.DeleteTrainerAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as ApiResponseDto<bool>;
            _unitOfWork.Clear();

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.False(await _context.Trainer.IgnoreQueryFilters().AnyAsync(t => t.Id == trainer.Id));
            Assert.False(await _context.Client.IgnoreQueryFilters().AnyAsync(c => c.TrainerId == trainer.Id));
            Assert.False(await _context.Notification.AnyAsync(n => n.TrainerId == trainer.Id));
            Assert.False(await _context.Payments.IgnoreQueryFilters().AnyAsync(p => p.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task TestDeleteTrainerSucceedsForTrainerWithNoDependenciesAsync()
        {
            // A brand-new trainer with no clients/payments/notifications must still be deletable -
            // the trainer row itself is the one guaranteed change, so Complete() must have work to do.
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            _unitOfWork.Clear();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _trainerController.DeleteTrainerAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as ApiResponseDto<bool>;
            _unitOfWork.Clear();

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.False(await _context.Trainer.IgnoreQueryFilters().AnyAsync(t => t.Id == trainer.Id));
        }

        [Fact]
        public async Task TestDeleteTrainerOnlyDeletesTheAuthenticatedTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(trainer, otherTrainer);
            await _unitOfWork.Complete();

            var otherClient = new Client { FirstName = "bob", Role = UserRole.Client, TrainerId = otherTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(otherClient);
            await _unitOfWork.Complete();
            _unitOfWork.Clear();

            AuthenticateAsTrainer(trainer.Id);
            await _trainerController.DeleteTrainerAsync();
            _unitOfWork.Clear();

            Assert.True(await _context.Trainer.AnyAsync(t => t.Id == otherTrainer.Id));
            Assert.True(await _context.Client.AnyAsync(c => c.Id == otherClient.Id));
        }
    }
}

