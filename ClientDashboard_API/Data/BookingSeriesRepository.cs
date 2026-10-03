using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookingSeriesRepository(DataContext context) : IBookingSeriesRepository
    {
        public async Task<List<BookingSeries>> GetAllBookingSeriesAsync()
        {
            return await context.BookingSeries.ToListAsync();
        }

        public async Task RemoveAllBookingSeriesForUserAsync(UserBase user)
        {
            var seriesToRemove = user.Role == UserRole.Trainer
                ? await context.BookingSeries.Where(b => b.TrainerId == user.Id).ToListAsync()
                : await context.BookingSeries.Where(b => b.ClientId == user.Id).ToListAsync();

            context.BookingSeries.RemoveRange(seriesToRemove);
        }
    }
}
