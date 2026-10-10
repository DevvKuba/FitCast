using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface IBookingMaterializationService
    {
        Task MaterializeBookingSeriesAsync(BookingSeries series);
    }
}
