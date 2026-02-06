using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class AutoPaymentCreationService(IUnitOfWork unitOfWork) : IAutoPaymentCreationService
    {
        public async Task<ApiResponseDto<string>> CreatePendingPaymentAsync(Trainer trainer, Client client)
        {
            var blockPrice = client.TotalBlockSessions * trainer.AverageSessionPrice;
            await unitOfWork.PaymentRepository.AddNewPaymentAsync(trainer, client, client.TotalBlockSessions ?? 0, blockPrice ?? 0m, DateOnly.FromDateTime(DateTime.Now), false);
            
            if(!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Did not succeed when creating pending payment for client: {client.FirstName}", Success = false };
            }
            return new ApiResponseDto<string> { Data = null, Message = $"Successfully created payment for {client.FirstName}", Success = true };
        }
    }
}
