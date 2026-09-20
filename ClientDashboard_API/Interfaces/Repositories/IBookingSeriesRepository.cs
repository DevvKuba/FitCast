using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IBookingSeriesRepository
    {
        Task RemoveAllBookingSeriesForUserAsync(UserBase user);
    }
}
