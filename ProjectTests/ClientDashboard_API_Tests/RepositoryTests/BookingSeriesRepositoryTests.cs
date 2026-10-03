using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class BookingSeriesRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly NotificationRecipientStatusRepository _notificationRecipientStatusRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly EmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly BookingSeriesRepository _bookingSeriesRepository;
        private readonly BookedSessionSlotRepository _bookedSessionSlotRepository;
        private readonly UnitOfWork _unitOfWork;

        public BookingSeriesRepositoryTests()
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
            _notificationRecipientStatusRepository = new NotificationRecipientStatusRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context, _mapper);
            _bookingSeriesRepository = new BookingSeriesRepository(_context);
            _bookedSessionSlotRepository = new BookedSessionSlotRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _notificationRecipientStatusRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository, _bookingSeriesRepository, _bookedSessionSlotRepository);
        }

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john")
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }

        private async Task<Client> SeedClientAsync(Trainer trainer, string firstName)
        {
            var client = new Client { FirstName = firstName, Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return client;
        }

        private async Task SeedSeriesAsync(Trainer trainer, Client client, string title = "weekly session")
        {
            await _context.BookingSeries.AddAsync(new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = title,
                Duration = 60,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                StartTime = new TimeOnly(18, 0),
                MaterializedUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90),
                Recurrence = SessionRecurrence.Weekly
            });
            await _unitOfWork.Complete();
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForTrainer_RemovesEverySeriesAcrossTheirClients()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            await SeedSeriesAsync(trainer, alice);
            await SeedSeriesAsync(trainer, alice, "one-off");
            await SeedSeriesAsync(trainer, bob);

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookingSeries.AnyAsync(b => b.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForClient_RemovesOnlyThatClientsSeries()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            await SeedSeriesAsync(trainer, alice);
            await SeedSeriesAsync(trainer, bob);

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(alice);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookingSeries.AnyAsync(b => b.ClientId == alice.Id));
            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.ClientId == bob.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForTrainer_LeavesOtherTrainersSeriesUntouched()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.TrainerId == otherTrainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_StagesNothingWhenUserHasNoSeries()
        {
            var trainer = await SeedTrainerAsync();

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);

            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_OnlyStagesRemovalUntilComplete()
        {
            // Matches every other repository here: the caller decides when to persist.
            var trainer = await SeedTrainerAsync();
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);

            Assert.True(_unitOfWork.HasChanges());
            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.TrainerId == trainer.Id));
        }
    }
}
