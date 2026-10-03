using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // The InMemory provider doesn't enforce foreign keys, so these tests can't prove SQL Server
    // would accept the trainer delete by calling SaveChanges alone. Instead they assert the actual
    // contract: once the helper has run, no row with a NoAction FK to the trainer (Client,
    // BookingSeries, Notification, Payment) still exists. Every check uses IgnoreQueryFilters so
    // soft-deleted clients and invisible payments can't hide from the assertion.
    public class TrainerDependencyRemovalHelperTests
    {
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly TrainerDependencyRemovalHelper _trainerRemovalHelper;

        public TrainerDependencyRemovalHelperTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            _trainerRemovalHelper = new TrainerDependencyRemovalHelper(_unitOfWork);
        }

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john")
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }

        private async Task<Client> SeedClientAsync(Trainer trainer, string firstName, bool isDeleted = false)
        {
            var client = new Client
            {
                FirstName = firstName,
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTime.UtcNow : null
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return client;
        }

        private async Task SeedNotificationAsync(Trainer trainer, int? clientId = null)
        {
            await _context.Notification.AddAsync(new Notification
            {
                TrainerId = trainer.Id,
                ClientId = clientId,
                Message = "notification",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            });
            await _unitOfWork.Complete();
        }

        private async Task SeedPaymentAsync(Trainer trainer, int? clientId = null, bool isVisible = true)
        {
            await _context.Payments.AddAsync(new Payment
            {
                TrainerId = trainer.Id,
                ClientId = clientId,
                Amount = 120,
                Currency = "GBP",
                NumberOfSessions = 4,
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                IsVisible = isVisible
            });
            await _unitOfWork.Complete();
        }

        private async Task SeedBookingSeriesAsync(Trainer trainer, Client client)
        {
            await _context.BookingSeries.AddAsync(new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                StartTime = new TimeOnly(18, 0),
                MaterializedUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90),
                Recurrence = SessionRecurrence.Weekly
            });
            await _unitOfWork.Complete();
        }

        // Detach everything seeded so the helper works against a fresh view of the data, the way it
        // does inside a real request - otherwise EF's client-side fixup on already-tracked dependents
        // can mask what the helper itself did or didn't stage.
        private async Task RunHelperAndSaveAsync(Trainer trainer)
        {
            _unitOfWork.Clear();
            await _trainerRemovalHelper.RemoveAllTrainerAssociatedDependenciesAsync(trainer);
            await _unitOfWork.Complete();
            _unitOfWork.Clear();
        }

        private Task<int> CountClientsReferencingAsync(int trainerId) =>
            _context.Client.IgnoreQueryFilters().CountAsync(c => c.TrainerId == trainerId);

        private Task<int> CountNotificationsReferencingAsync(int trainerId) =>
            _context.Notification.CountAsync(n => n.TrainerId == trainerId);

        private Task<int> CountPaymentsReferencingAsync(int trainerId) =>
            _context.Payments.IgnoreQueryFilters().CountAsync(p => p.TrainerId == trainerId);

        private Task<int> CountBookingSeriesReferencingAsync(int trainerId) =>
            _context.BookingSeries.CountAsync(b => b.TrainerId == trainerId);

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_RemovesClientsNotificationsAndPayments()
        {
            var trainer = await SeedTrainerAsync();
            var clientA = await SeedClientAsync(trainer, "alice");
            var clientB = await SeedClientAsync(trainer, "bob");
            await SeedNotificationAsync(trainer, clientA.Id);
            await SeedNotificationAsync(trainer);
            await SeedPaymentAsync(trainer, clientA.Id);
            await SeedPaymentAsync(trainer, clientB.Id);

            await RunHelperAndSaveAsync(trainer);

            Assert.Equal(0, await CountClientsReferencingAsync(trainer.Id));
            Assert.Equal(0, await CountNotificationsReferencingAsync(trainer.Id));
            Assert.Equal(0, await CountPaymentsReferencingAsync(trainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_RemovesBookingSeries()
        {
            // Trainer -> BookingSeries is NoAction, so any remaining series blocks the trainer delete.
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            await SeedBookingSeriesAsync(trainer, client);

            await RunHelperAndSaveAsync(trainer);

            Assert.Equal(0, await CountBookingSeriesReferencingAsync(trainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_RemovesSoftDeletedClients()
        {
            // A soft-deleted client the cleanup job hasn't hard-deleted yet still holds TrainerId,
            // and Trainer -> Client is NoAction - the global !IsDeleted query filter must not hide it.
            var trainer = await SeedTrainerAsync();
            await SeedClientAsync(trainer, "alice", isDeleted: true);

            await RunHelperAndSaveAsync(trainer);

            Assert.Equal(0, await CountClientsReferencingAsync(trainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_RemovesInvisiblePayments()
        {
            // Payment.TrainerId is a required NoAction FK - a payment hidden by the IsVisible filter
            // still blocks the trainer delete just as much as a visible one.
            var trainer = await SeedTrainerAsync();
            await SeedPaymentAsync(trainer, isVisible: false);

            await RunHelperAndSaveAsync(trainer);

            Assert.Equal(0, await CountPaymentsReferencingAsync(trainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_LeavesOtherTrainersDataUntouched()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");

            await SeedClientAsync(trainer, "alice");
            await SeedNotificationAsync(trainer);
            await SeedPaymentAsync(trainer);

            var otherClient = await SeedClientAsync(otherTrainer, "bob");
            await SeedNotificationAsync(otherTrainer, otherClient.Id);
            await SeedPaymentAsync(otherTrainer, otherClient.Id);
            await SeedBookingSeriesAsync(otherTrainer, otherClient);

            await RunHelperAndSaveAsync(trainer);

            Assert.Equal(1, await CountClientsReferencingAsync(otherTrainer.Id));
            Assert.Equal(1, await CountNotificationsReferencingAsync(otherTrainer.Id));
            Assert.Equal(1, await CountPaymentsReferencingAsync(otherTrainer.Id));
            Assert.Equal(1, await CountBookingSeriesReferencingAsync(otherTrainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_StagesNothingForTrainerWithNoDependencies()
        {
            var trainer = await SeedTrainerAsync();
            _unitOfWork.Clear();

            await _trainerRemovalHelper.RemoveAllTrainerAssociatedDependenciesAsync(trainer);

            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_DoesNotRemoveTheTrainerItself()
        {
            // The helper's job is only to clear the way - removing the trainer stays the caller's
            // responsibility, so the two can be staged together before a single Complete().
            var trainer = await SeedTrainerAsync();
            await SeedClientAsync(trainer, "alice");

            await RunHelperAndSaveAsync(trainer);

            Assert.True(await _context.Trainer.AnyAsync(t => t.Id == trainer.Id));
        }

        [Fact]
        public async Task RemoveAllTrainerAssociatedDependenciesAsync_ThenDeleteTrainer_LeavesNoTrainerOrBlockingRows()
        {
            // The full intended flow: helper + DeleteTrainer staged together, one Complete().
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            await SeedNotificationAsync(trainer, client.Id);
            await SeedPaymentAsync(trainer, client.Id);
            await SeedBookingSeriesAsync(trainer, client);
            _unitOfWork.Clear();

            var trackedTrainer = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);
            await _trainerRemovalHelper.RemoveAllTrainerAssociatedDependenciesAsync(trackedTrainer!);
            _unitOfWork.TrainerRepository.DeleteTrainer(trackedTrainer!);
            await _unitOfWork.Complete();
            _unitOfWork.Clear();

            Assert.False(await _context.Trainer.IgnoreQueryFilters().AnyAsync(t => t.Id == trainer.Id));
            Assert.Equal(0, await CountClientsReferencingAsync(trainer.Id));
            Assert.Equal(0, await CountNotificationsReferencingAsync(trainer.Id));
            Assert.Equal(0, await CountPaymentsReferencingAsync(trainer.Id));
            Assert.Equal(0, await CountBookingSeriesReferencingAsync(trainer.Id));
        }
    }
}
