using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookedSessionSlotRepository(DataContext context, IMapper mapper) : IBookedSessionSlotRepository
    {
        public async Task<BookedSessionSlot?> GetTrainerBookedSessionSlotByIdAsync(int trainerId, int sessionId)
        {
            return await context.BookedSessionsSlot.FirstOrDefaultAsync(s => s.Id == sessionId && s.TrainerId == trainerId);
        }
        public async Task<List<BookedSessionSlot>> GetTrainerBookedSessionSlotsInRangeAsync(int trainerId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            return await context.BookedSessionsSlot
                .Where(s => s.TrainerId == trainerId && s.StartDateTime >= rangeStartUtc && s.StartDateTime < rangeEndUtc)
                .OrderBy(s => s.StartDateTime)
                .ToListAsync();
        }

        public void UpdateBookedSessionSlot(BookedSessionSlot bookedSessionSlot, BookedSessionSlotDto updatedBookedSessionSlot)
        {
            mapper.Map(updatedBookedSessionSlot, bookedSessionSlot);
        }

        public async Task AddBookingSlotForTrainerAsync(Trainer trainer, BookedSessionSlotDto bookedSessionSlotDto)
        {
            var bookedSessionSlot = mapper.Map<BookedSessionSlot>(bookedSessionSlotDto);
            bookedSessionSlot.TrainerId = trainer.Id;

            await context.AddAsync(bookedSessionSlot);
        }

        public async Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user)
        {
            var slotsToRemove = user.Role == Enums.UserRole.Trainer ?
                await context.BookedSessionsSlot.Where(s => s.TrainerId == user.Id).ToListAsync() :
                await context.BookedSessionsSlot.Where(s => s.ClientId == user.Id).ToListAsync();

            context.BookedSessionsSlot.RemoveRange(slotsToRemove);
        }

        public void RemoveBookedSessionSlot(BookedSessionSlot sessionSlot)
        {
            context.Remove(sessionSlot);
        }
    }
}
