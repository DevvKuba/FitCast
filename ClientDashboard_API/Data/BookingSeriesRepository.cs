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
            return await context.BookingSeries.Include(s => s.Trainer).ToListAsync();
        }

        public async Task<BookingSeries?> GetTrainerBookingSeriesByIdAsync(int trainerId, int bookingSeriesId)
        {
            return await context.BookingSeries.FirstOrDefaultAsync(b => b.Id == bookingSeriesId && b.TrainerId == trainerId);
        }

        public async Task<List<BookingSeriesDto>> GetTrainerBookingSeriesDtosAsync(int trainerId)
        {
            return await BuildTrainerBookingSeriesQuery(trainerId)
                .OrderBy(b => b.ClientName)
                .ThenBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<BookingSeriesDto?> GetTrainerBookingSeriesDtoByIdAsync(int trainerId, int bookingSeriesId)
        {
            return await BuildTrainerBookingSeriesQuery(trainerId).FirstOrDefaultAsync(b => b.Id == bookingSeriesId);
        }

        public async Task<BookingSeries?> GetBookingSeriesByIdWithTrainer(int trainerId, int bookingSeriesId)
        {
            return await context.BookingSeries
                .Where(b => b.Id == bookingSeriesId && b.TrainerId == trainerId)
                .Include(b => b.Trainer)
                .FirstOrDefaultAsync();
        }

        public void UpdateBookingSeriesAndResetMaterializedUntil(BookingSeries bookingSeries, BookingSeriesUpdateDto updatedBookingSeries)
        {
            mapper.Map(updatedBookingSeries, bookingSeries);
            bookingSeries.MaterializedUntil = bookingSeries.StartDate.AddDays(-(int)bookingSeries.Recurrence);
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

        private IQueryable<BookingSeriesDto> BuildTrainerBookingSeriesQuery(int trainerId)
        {
            return context.BookingSeries
                .Where(b => b.TrainerId == trainerId)
                .Select(b => new BookingSeriesDto
                {
                    Id = b.Id,
                    ClientId = b.ClientId,
                    ClientName = b.Client.FirstName,
                    Title = b.Title,
                    Description = b.Description,
                    Duration = b.Duration,
                    StartDate = b.StartDate,
                    StartTime = b.StartTime,
                    Recurrence = b.Recurrence
                });
        }
    }
}
