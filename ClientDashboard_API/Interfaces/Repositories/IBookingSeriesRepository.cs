using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IBookingSeriesRepository
    {
        Task<List<BookingSeries>> GetAllBookingSeriesAsync();

        Task<BookingSeries?> GetTrainerBookingSeriesByIdAsync(int trainerId, int bookingSeriesId);

        Task<BookingSeries?> GetBookingSeriesByIdWithTrainer(int trainerId, int bookingSeriesId);

        void UpdateBookingSeriesStartDate(BookingSeries bookingSeries, BookingSeriesStartDateUpdateDto updatedStartDate);

        void DeleteBookingSeries(BookingSeries bookingSeries);

        Task<BookingSeries> CreateBookingSeriesUnderTrainerAsync(Trainer trainer, BookingSeriesCreateDto bookingSeriesDetails);

        Task RemoveAllBookingSeriesForUserAsync(UserBase user);
    }
}
