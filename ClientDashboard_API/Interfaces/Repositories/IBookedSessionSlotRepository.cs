using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IBookedSessionSlotRepository
    {
        Task<BookedSessionSlot?> GetBookedSessionSlotByIdAsync(int sessionId);
        Task AddBookingSlotForTrainerAsync(Trainer trainer, BookedSessionSlotAddDto bookedSessionSlotDto);
        Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user);

        void RemoveBookedSessionSlot(BookedSessionSlot sessionSlot);
    }
}
