using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookedSessionSlotRepository(DataContext context, IMapper mapper) : IBookedSessionSlotRepository
    {
        public async Task<BookedSessionSlot?> GetBookedSessionSlotByIdAsync(int sessionId)
        {
            return await context.BookedSessionsSlot.Where(s => s.Id == sessionId).FirstAsync();
        }
        public async Task AddBookingSlotForTrainerAsync(Trainer trainer, BookedSessionSlotAddDto bookedSessionSlotDto)
        {
            var bookedSessionSlot = mapper.Map<BookedSessionSlot>(bookedSessionSlotDto);

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
