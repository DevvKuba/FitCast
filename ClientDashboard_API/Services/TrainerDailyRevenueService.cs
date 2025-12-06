using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerDailyRevenueService(IUnitOfWork unitOfWork) : ITrainerDailyRevenueService
    {
        // revenue today would just be getting all confirmed trainer payments form todays Date
        // retrieving their amount

        // MonthlyRevenueThusFar - need to identify the month, check from the first - Current Date
        // all the confirmed payments

        // TotalSessionsThisMonth - same thing identify the 1t of current month
        // check for all clients under trainer, how many logged sessions are in between 1st - Current Date

        // NewClientsThisMonth - get access to the past month - client count and get get current client count
        // current - past = res

        // active clients , all clients with active status under trainer

        // average session price just takes the trainers set price - no need for excessive calculations

        public Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer)
        {
            throw new NotImplementedException();
        }

        public DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate)
        {
            var firstDayOfGivenMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
            return firstDayOfGivenMonth;
        }
    }
}
