using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class AutoPaymentCreationService(IUnitOfWork unitOfWork) : IAutoPaymentCreationService
    {
        // goal is to create a 'Pending' Trainer payment using specific information from trainer and client
        // trainer: trainerId, amount (calculated from trainer session rate * numberOfSessions, Currency (chosen by trainer)

        //client: clientId, NumberOfSessions (current block)


    }
}
