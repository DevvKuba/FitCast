using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class BookedSessionSlotRepositoryTests
    {
        private readonly DataContext _context;
        private readonly BookedSessionSlotRepository _bookedSessionSlotRepository;
        private readonly UnitOfWork _unitOfWork;

        // Time zone change tests use fixed winter dates, when London is UTC+0 and New York is UTC-5,
        // and pass an explicit fromUtc so results don't depend on when the suite runs.
        private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.London);
        private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.NewYork);
        private static readonly DateTime ShiftFromUtc = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public BookedSessionSlotRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _bookedSessionSlotRepository = testUnitOfWork.bookedSessionSlotRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
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

        private async Task<BookedSessionSlot> SeedSlotAsync(Trainer trainer, Client client, BookingSeries? series = null, DateTime? startUtc = null)
        {
            var start = startUtc ?? DateTime.UtcNow.Date.AddDays(1).AddHours(18);
            var slot = new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                BookingSeriesId = series?.Id,
                Title = series is null ? "ad-hoc session" : series.Title,
                StartDateTime = start,
                EndDateTime = start.AddHours(1),
                Status = SessionStatus.Scheduled
            };
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();
            return slot;
        }

        private static BookedSessionSlotDto SlotDto(int clientId, DateTime startUtc) => new()
        {
            ClientId = clientId,
            Title = "updated session",
            Description = "updated notes",
            StartDateTime = startUtc,
            EndDateTime = startUtc.AddMinutes(45)
        };

        [Fact]
        public async Task GetTrainerBookedSessionSlotByIdAsync_ReturnsTheTrainersOwnSlot()
        {
            var trainer = await SeedTrainerAsync();
            var slot = await SeedSlotAsync(trainer, await SeedClientAsync(trainer, "alice"));

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotByIdAsync(trainer.Id, slot.Id);

            Assert.NotNull(result);
            Assert.Equal(slot.Id, result.Id);
        }

        [Fact]
        public async Task GetTrainerBookedSessionSlotByIdAsync_ReturnsNullForAnotherTrainersSlot()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSlot = await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotByIdAsync(trainer.Id, otherSlot.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTrainerBookedSessionSlotByIdAsync_ReturnsNullWhenSlotDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotByIdAsync(trainer.Id, 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTrainerBookedSessionSlotsInRangeAsync_ReturnsOnlySlotsInsideTheRange_OrderedByStart()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var rangeStart = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            var rangeEnd = rangeStart.AddDays(90);

            var later = await SeedSlotAsync(trainer, alice, startUtc: rangeStart.AddDays(30));
            var atStart = await SeedSlotAsync(trainer, alice, startUtc: rangeStart);
            await SeedSlotAsync(trainer, alice, startUtc: rangeStart.AddMinutes(-1));
            await SeedSlotAsync(trainer, alice, startUtc: rangeEnd);

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotsInRangeAsync(trainer.Id, rangeStart, rangeEnd);

            // Start is inclusive, end is exclusive.
            Assert.Equal([atStart.Id, later.Id], result.Select(s => s.Id));
        }

        [Fact]
        public async Task GetTrainerBookedSessionSlotsInRangeAsync_ExcludesOtherTrainersSlots()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var rangeStart = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            await SeedSlotAsync(trainer, await SeedClientAsync(trainer, "alice"), startUtc: rangeStart.AddDays(1));
            await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"), startUtc: rangeStart.AddDays(1));

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotsInRangeAsync(trainer.Id, rangeStart, rangeStart.AddDays(90));

            var single = Assert.Single(result);
            Assert.Equal("alice", single.ClientName);
        }

        [Fact]
        public async Task GetTrainerBookedSessionSlotsInRangeAsync_MapsEverySlotFieldIncludingClientName()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            var rangeStart = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            var slot = await SeedSlotAsync(trainer, alice, series, rangeStart.AddDays(1));

            var result = await _bookedSessionSlotRepository.GetTrainerBookedSessionSlotsInRangeAsync(trainer.Id, rangeStart, rangeStart.AddDays(7));

            var dto = Assert.Single(result);
            Assert.Equal(slot.Id, dto.Id);
            Assert.Equal(alice.Id, dto.ClientId);
            Assert.Equal("alice", dto.ClientName);
            Assert.Equal(series.Id, dto.BookingSeriesId);
            Assert.Equal(slot.Title, dto.Title);
            Assert.Equal(slot.StartDateTime, dto.StartDateTime);
            Assert.Equal(slot.EndDateTime, dto.EndDateTime);
            Assert.Equal(SessionStatus.Scheduled, dto.Status);
        }

        [Fact]
        public async Task UpdateBookedSessionSlot_MapsDtoFields_AndKeepsTrainerSeriesAndStatus()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            var series = await SeedSeriesAsync(trainer, alice);
            var slot = await SeedSlotAsync(trainer, alice, series);
            var newStart = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

            _bookedSessionSlotRepository.UpdateBookedSessionSlot(slot, SlotDto(bob.Id, newStart));
            await _unitOfWork.Complete();

            var saved = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(bob.Id, saved.ClientId);
            Assert.Equal("updated session", saved.Title);
            Assert.Equal("updated notes", saved.Description);
            Assert.Equal(newStart, saved.StartDateTime);
            Assert.Equal(newStart.AddMinutes(45), saved.EndDateTime);
            // Fields the DTO doesn't carry stay as they were.
            Assert.Equal(trainer.Id, saved.TrainerId);
            Assert.Equal(series.Id, saved.BookingSeriesId);
            Assert.Equal(SessionStatus.Scheduled, saved.Status);
        }

        [Fact]
        public async Task AddBookingSlotForTrainerAsync_AssignsTrainerAndCreatesStandaloneSlot()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var start = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

            await _bookedSessionSlotRepository.AddBookingSlotForTrainerAsync(trainer, SlotDto(alice.Id, start));
            await _unitOfWork.Complete();

            var saved = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(trainer.Id, saved.TrainerId);
            Assert.Equal(alice.Id, saved.ClientId);
            Assert.Null(saved.BookingSeriesId);
            Assert.Equal(SessionStatus.Scheduled, saved.Status);
            Assert.Equal(start, saved.StartDateTime);
        }

        [Fact]
        public async Task AddBookedMaterializedSlotsAsync_LinksSlotToAnUnsavedSeriesThroughTheNavigation()
        {
            // A new series has Id 0 until Complete() - the slot must be linked by reference, and the
            // series must be inserted once, not duplicated by the mapping.
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = alice.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = new DateOnly(2026, 10, 11),
                StartTime = new TimeOnly(9, 0),
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            var start = new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc);

            await _bookedSessionSlotRepository.AddBookedMaterializedSlotsAsync(new MaterializedBookedSessionSlotDto
            {
                TrainerId = trainer.Id,
                ClientId = alice.Id,
                Title = series.Title,
                StartDateTime = start,
                EndDateTime = start.AddHours(1),
                BookingSeries = series
            });
            await _unitOfWork.Complete();

            var saved = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(1, await _context.BookingSeries.CountAsync());
            Assert.NotEqual(0, series.Id);
            Assert.Equal(series.Id, saved.BookingSeriesId);
            Assert.Equal(trainer.Id, saved.TrainerId);
            Assert.Equal(SessionStatus.Scheduled, saved.Status);
        }

        [Fact]
        public async Task RemoveBookedSessionSlotsAtCutOffDateAsync_RemovesScheduledSeriesSlotsFromTheCutOff()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            var cutOff = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var beforeCutOff = await SeedSlotAsync(trainer, alice, series, cutOff.AddDays(-1));
            var atCutOff = await SeedSlotAsync(trainer, alice, series, cutOff);
            var afterCutOff = await SeedSlotAsync(trainer, alice, series, cutOff.AddDays(7));

            await _bookedSessionSlotRepository.RemoveBookedSessionSlotsAtCutOffDateAsync(series.Id, cutOff);
            await _unitOfWork.Complete();

            // Cut-off is inclusive.
            Assert.Equal([beforeCutOff.Id], await _context.BookedSessionsSlot.Select(s => s.Id).ToListAsync());
        }

        [Theory]
        [InlineData(SessionStatus.Completed)]
        [InlineData(SessionStatus.Cancelled)]
        public async Task RemoveBookedSessionSlotsAtCutOffDateAsync_KeepsSlotsThatAreNoLongerScheduled(SessionStatus status)
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            var cutOff = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var slot = await SeedSlotAsync(trainer, alice, series, cutOff.AddDays(7));
            slot.Status = status;
            await _unitOfWork.Complete();

            await _bookedSessionSlotRepository.RemoveBookedSessionSlotsAtCutOffDateAsync(series.Id, cutOff);
            await _unitOfWork.Complete();

            Assert.True(await _context.BookedSessionsSlot.AnyAsync(s => s.Id == slot.Id));
        }

        [Fact]
        public async Task RemoveBookedSessionSlotsAtCutOffDateAsync_LeavesOtherSeriesAndStandaloneSlots()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            var otherSeries = await SeedSeriesAsync(trainer, alice);
            var cutOff = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            await SeedSlotAsync(trainer, alice, series, cutOff.AddDays(1));
            var otherSeriesSlot = await SeedSlotAsync(trainer, alice, otherSeries, cutOff.AddDays(1));
            var standaloneSlot = await SeedSlotAsync(trainer, alice, startUtc: cutOff.AddDays(1));

            await _bookedSessionSlotRepository.RemoveBookedSessionSlotsAtCutOffDateAsync(series.Id, cutOff);
            await _unitOfWork.Complete();

            Assert.Equal([otherSeriesSlot.Id, standaloneSlot.Id], await _context.BookedSessionsSlot.OrderBy(s => s.Id).Select(s => s.Id).ToListAsync());
        }

        [Fact]
        public async Task RemoveBookedSessionSlot_RemovesOnlyThatSlot()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var toRemove = await SeedSlotAsync(trainer, alice);
            var toKeep = await SeedSlotAsync(trainer, alice);

            _bookedSessionSlotRepository.RemoveBookedSessionSlot(toRemove);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookedSessionsSlot.AnyAsync(s => s.Id == toRemove.Id));
            Assert.True(await _context.BookedSessionsSlot.AnyAsync(s => s.Id == toKeep.Id));
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

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_KeepsLocalClockTimeAndDuration()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var slot = await SeedSlotAsync(trainer, client, startUtc: new DateTime(2027, 1, 13, 9, 0, 0, DateTimeKind.Utc)); // 09:00 London

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);
            await _unitOfWork.Complete();

            Assert.Equal(new DateTime(2027, 1, 13, 14, 0, 0, DateTimeKind.Utc), slot.StartDateTime); // 09:00 New York
            Assert.Equal(new DateTime(2027, 1, 13, 15, 0, 0, DateTimeKind.Utc), slot.EndDateTime);
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_ShiftsSeriesAndStandaloneSlots()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, client);
            var seriesSlot = await SeedSlotAsync(trainer, client, series, new DateTime(2027, 1, 13, 18, 0, 0, DateTimeKind.Utc));
            var standaloneSlot = await SeedSlotAsync(trainer, client, startUtc: new DateTime(2027, 1, 14, 7, 0, 0, DateTimeKind.Utc));

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);
            await _unitOfWork.Complete();

            Assert.Equal(new DateTime(2027, 1, 13, 23, 0, 0, DateTimeKind.Utc), seriesSlot.StartDateTime);
            Assert.Equal(new DateTime(2027, 1, 14, 12, 0, 0, DateTimeKind.Utc), standaloneSlot.StartDateTime);
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_LeavesCompletedAndCancelledSlotsUntouched()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var originalStart = new DateTime(2027, 1, 13, 9, 0, 0, DateTimeKind.Utc);
            var completedSlot = await SeedSlotAsync(trainer, client, startUtc: originalStart);
            var cancelledSlot = await SeedSlotAsync(trainer, client, startUtc: originalStart.AddDays(1));
            completedSlot.Status = SessionStatus.Completed;
            cancelledSlot.Status = SessionStatus.Cancelled;
            await _unitOfWork.Complete();

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);

            Assert.Equal(originalStart, completedSlot.StartDateTime);
            Assert.Equal(originalStart.AddDays(1), cancelledSlot.StartDateTime);
            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_LeavesSlotsBeforeFromUtcUntouched()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var pastStart = ShiftFromUtc.AddDays(-7).AddHours(9);
            var pastSlot = await SeedSlotAsync(trainer, client, startUtc: pastStart);

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);

            Assert.Equal(pastStart, pastSlot.StartDateTime);
            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_LeavesOtherTrainersSlotsUntouched()
        {
            var trainer = await SeedTrainerAsync();
            var otherTrainer = await SeedTrainerAsync("mike");
            var otherStart = new DateTime(2027, 1, 13, 9, 0, 0, DateTimeKind.Utc);
            var otherSlot = await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"), startUtc: otherStart);

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);

            Assert.Equal(otherStart, otherSlot.StartDateTime);
            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_MovesTimeInNewZonesClockChangeGapForwardAnHour()
        {
            // New York springs forward 02:00 -> 03:00 on 14 March 2027, while London is still UTC+0,
            // so a 02:30 London session has no matching 02:30 in New York that day
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var slot = await SeedSlotAsync(trainer, client, startUtc: new DateTime(2027, 3, 14, 2, 30, 0, DateTimeKind.Utc));

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);
            await _unitOfWork.Complete();

            Assert.Equal(new DateTime(2027, 3, 14, 7, 30, 0, DateTimeKind.Utc), slot.StartDateTime); // 03:30 New York (UTC-4)
            Assert.Equal(TimeSpan.FromHours(1), slot.EndDateTime - slot.StartDateTime);
        }

        [Fact]
        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync_OnlyStagesChangesUntilComplete()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var originalStart = new DateTime(2027, 1, 13, 9, 0, 0, DateTimeKind.Utc);
            var slot = await SeedSlotAsync(trainer, client, startUtc: originalStart);

            await _bookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, London, NewYork, ShiftFromUtc);

            Assert.True(_unitOfWork.HasChanges());
            var persistedStart = await _context.BookedSessionsSlot.AsNoTracking().Where(s => s.Id == slot.Id).Select(s => s.StartDateTime).SingleAsync();
            Assert.Equal(originalStart, persistedStart);
        }
    }
}
