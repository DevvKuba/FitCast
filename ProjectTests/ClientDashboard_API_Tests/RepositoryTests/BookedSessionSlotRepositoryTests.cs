using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class BookedSessionSlotRepositoryTests
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

        public BookedSessionSlotRepositoryTests()
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

        private async Task<BookingSeries> SeedSeriesAsync(Trainer trainer, Client client)
        {
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                StartTime = new TimeOnly(18, 0),
                MaterializedUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90),
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            await _unitOfWork.Complete();
            return series;
        }

        private async Task SeedSlotAsync(Trainer trainer, Client client, BookingSeries? series = null)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(18);
            await _context.BookedSessionsSlot.AddAsync(new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                BookingSeriesId = series?.Id,
                Title = series is null ? "ad-hoc session" : series.Title,
                StartDateTime = start,
                EndDateTime = start.AddHours(1),
                Status = SessionStatus.Scheduled
            });
            await _unitOfWork.Complete();
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_ForTrainer_RemovesSeriesAndStandaloneSlots()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            var series = await SeedSeriesAsync(trainer, alice);
            await SeedSlotAsync(trainer, alice, series);
            await SeedSlotAsync(trainer, alice);
            await SeedSlotAsync(trainer, bob);

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookedSessionsSlot.AnyAsync(s => s.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_ForClient_RemovesOnlyThatClientsSlots()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            await SeedSlotAsync(trainer, alice);
            await SeedSlotAsync(trainer, alice, await SeedSeriesAsync(trainer, alice));
            await SeedSlotAsync(trainer, bob);

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(alice);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookedSessionsSlot.AnyAsync(s => s.ClientId == alice.Id));
            Assert.Equal(1, await _context.BookedSessionsSlot.CountAsync(s => s.ClientId == bob.Id));
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_DoesNotRemoveTheParentSeries()
        {
            // Slots are the child side - clearing them must leave their BookingSeries in place.
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            await SeedSlotAsync(trainer, alice, series);

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.True(await _context.BookingSeries.AnyAsync(b => b.Id == series.Id));
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_ForTrainer_LeavesOtherTrainersSlotsUntouched()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            await SeedSlotAsync(trainer, await SeedClientAsync(trainer, "alice"));
            await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.Equal(1, await _context.BookedSessionsSlot.CountAsync(s => s.TrainerId == otherTrainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_StagesNothingWhenUserHasNoSlots()
        {
            var trainer = await SeedTrainerAsync();

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);

            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task RemoveAllBookedSessionSlotsForUserAsync_OnlyStagesRemovalUntilComplete()
        {
            var trainer = await SeedTrainerAsync();
            await SeedSlotAsync(trainer, await SeedClientAsync(trainer, "alice"));

            await _bookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);

            Assert.True(_unitOfWork.HasChanges());
            Assert.Equal(1, await _context.BookedSessionsSlot.CountAsync(s => s.TrainerId == trainer.Id));
        }
    }
}
