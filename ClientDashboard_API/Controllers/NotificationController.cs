using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class NotificationController(IUnitOfWork unitOfWork, IMessageService messageService) : BaseAPIController
    {
        [HttpPost("SendTrainerBlockCompletionReminder")]
        public async Task<ActionResult<ApiResponseDto<string>>> TrainerBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            messageService.InitialiseBaseTwillioClient();
            var SENDER_PHONE_NUMBER = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");

            Trainer trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Trainer with id: {trainerId} not retrieved successfully to send message", Success = false };
            }

            Client client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not retrieved successfully to send message", Success = false };
            }

            var notificationMessage = $"{client.FirstName}'s monthly sessions have come to an end,\n" +
                $"remember to message them in regards of a new payment.";

            messageService.SendSMSMessage(trainer, client: null, SENDER_PHONE_NUMBER!, notificationMessage);

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage,
                reminderType: "Trainer Client Block termination", sentThrough: "SMS");

            if (!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was unsuccessful", Success = false };
            }
            return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was successful", Success = true };

        }

        [HttpPost("SendClientBlockCompletionReminder")]
        public async Task<ActionResult<ApiResponseDto<string>>> ClientBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            messageService.InitialiseBaseTwillioClient();
            var SENDER_PHONE_NUMBER = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");

            Trainer trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Trainer with id: {trainerId} not retrieved successfully to send message", Success = false };
            }

            Client client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not retrieved successfully to send message", Success = false };

            }

            var notificationMessage = $"Hey {client.FirstName}! this is {trainer.FirstName} just wanted to" +
             "inform you that our monthly sessions have come to an end,\n" +
                $"If you could place a block payment before our next session that would be great.";

            messageService.SendSMSMessage(trainer: null, client, SENDER_PHONE_NUMBER!, notificationMessage);

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage,
                reminderType: "Trainer Client Block termination", sentThrough: "SMS");

            if (!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was unsuccessful", Success = false };
            }
            return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was successful", Success = true };
        }

    }
}
