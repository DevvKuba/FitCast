using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookedSessionSlotRepository(DataContext context) : IBookedSessionSlotRepository
    {
        public async Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user)
        {
            var slotsToRemove = user.Role == Enums.UserRole.Trainer ?
                await context.BookedSessionsSlot.Where(s => s.TrainerId == user.Id).ToListAsync() :
                await context.BookedSessionsSlot.Where(s => s.ClientId == user.Id).ToListAsync();

            context.BookedSessionsSlot.RemoveRange(slotsToRemove);
        }
    }
}
