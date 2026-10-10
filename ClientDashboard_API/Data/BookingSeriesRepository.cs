using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookingSeriesRepository(DataContext context, IMapper mapper) : IBookingSeriesRepository
    {
        public async Task<List<BookingSeries>> GetAllBookingSeriesAsync()
        {
            return await context.BookingSeries.ToListAsync();
        }

        public async Task<BookingSeries?> GetTrainerBookingSeriesByIdAsync(int trainerId, int bookingSeriesId)
        {
            return await context.BookingSeries.FirstOrDefaultAsync(b => b.Id == bookingSeriesId && b.TrainerId == trainerId);
        }

        public async Task<BookingSeries?> GetBookingSeriesByIdWithTrainer(int trainerId, int bookingSeriesId)
        {
            return await context.BookingSeries
                .Where(b => b.Id == bookingSeriesId && b.TrainerId == trainerId)
                .Include(b => b.Trainer)
                .FirstOrDefaultAsync();
        }

        public void UpdateBookingSeriesStartDate(BookingSeries bookingSeries, BookingSeriesStartDateUpdateDto updatedStartDate)
        {
            mapper.Map(updatedStartDate, bookingSeries);
        }

        public void DeleteBookingSeries(BookingSeries bookingSeries)
        {
            context.BookingSeries.Remove(bookingSeries);
        }

        public async Task<BookingSeries> CreateBookingSeriesUnderTrainerAsync(Trainer trainer, BookingSeriesCreateDto bookingSeriesDetails)
        {
            var bookingSeries = mapper.Map<BookingSeries>(bookingSeriesDetails);
            bookingSeries.TrainerId = trainer.Id;
            bookingSeries.MaterializedUntil = bookingSeries.StartDate.AddDays(-(int)bookingSeries.Recurrence);

            await context.AddAsync(bookingSeries);

            return bookingSeries;
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
