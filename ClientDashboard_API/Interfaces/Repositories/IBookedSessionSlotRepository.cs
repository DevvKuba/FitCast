using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IBookedSessionSlotRepository
    {
        Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user);
    }
}
