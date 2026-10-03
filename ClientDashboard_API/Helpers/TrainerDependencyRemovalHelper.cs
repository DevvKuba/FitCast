using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;

namespace ClientDashboard_API.Helpers
{
    public class TrainerDependencyRemovalHelper(IUnitOfWork unitOfWork) : ITrainerDependencyRemovalHelper
    {
        public async Task RemoveAllTrainerAssociatedDependenciesAsync(Trainer trainer)
        {
            var trainerClients = await unitOfWork.ClientRepository.GetAllTrainerClientsIncludingSoftDeleteAsync(trainer.Id);
            
            var trainerNotifications = await unitOfWork.NotificationRepository.ReturnAllTrainerNotificationsAsync(trainer);

            var trainerPayments = await unitOfWork.PaymentRepository.GetAllTrainerPaymentsIncludingInvisibleStatusAsync(trainer);

            await unitOfWork.BookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);

            await unitOfWork.BookedSessionSlotRepository.RemoveAllBookedSessionSlotsForUserAsync(trainer);

            unitOfWork.ClientRepository.RemoveClients(trainerClients);
            unitOfWork.NotificationRepository.DeleteNotifications(trainerNotifications);
            unitOfWork.PaymentRepository.DeletePayments(trainerPayments);
        }
    }
}
