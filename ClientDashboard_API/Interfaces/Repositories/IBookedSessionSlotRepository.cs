using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IBookedSessionSlotRepository
    {
        Task<BookedSessionSlot?> GetBookedSessionSlotByIdAsync(int sessionId);
        Task<List<BookedSessionSlot>> GetTrainerBookedSessionSlotsInRangeAsync(int trainerId, DateTime rangeStartUtc, DateTime rangeEndUtc);
        void UpdateBookedSessionSlot(BookedSessionSlot bookedSessionSlot, BookedSessionSlotDto updatedBookedSessionSlot);
        Task AddBookingSlotForTrainerAsync(Trainer trainer, BookedSessionSlotDto bookedSessionSlotDto);
        Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user);

        void RemoveBookedSessionSlot(BookedSessionSlot sessionSlot);
    }
}
