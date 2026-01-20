using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class NotificationService(IUnitOfWork unitOfWork, IMessageService messageService) : INotificationService
    {
        public async Task<ApiResponseDto<string>> SendTrainerReminderAsync(int trainerId, int clientId)
        {
            messageService.InitialiseBaseTwillioClient();
            var SENDER_PHONE_NUMBER = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");

            Trainer? trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(trainerId);

            if (trainer == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Trainer with id: {trainerId} not retrieved successfully to send message", Success = false };
            }

            Client? client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not retrieved successfully to send message", Success = false };
            }

            var notificationMessage = $"{client.FirstName}'s monthly sessions have come to an end,\n" +
                $"remember to message them in regards of a new monthly payment.";

            messageService.SendSMSMessage(trainer, client: null, SENDER_PHONE_NUMBER!, notificationMessage);

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage,
                reminderType: Enums.NotificationType.TrainerBlockCompletionReminder,
                sentThrough: Enums.CommunicationType.SMS);

            if (!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was unsuccessful", Success = false };
            }
            return new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"Saving notification message: {notificationMessage} was successful", Success = true };
        }

        public async Task<ApiResponseDto<string>> SendClientReminderAsync(int trainerId, int clientId)
        {
            messageService.InitialiseBaseTwillioClient();
            var SENDER_PHONE_NUMBER = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");

            Trainer? trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(trainerId);

            if (trainer == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Trainer with id: {trainerId} not retrieved successfully to send message", Success = false };
            }

            Client? client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client == null)
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not retrieved successfully to send message", Success = false };
            }

            var notificationMessage = $"Hey {client.FirstName}! this is {trainer.FirstName} just wanted to" +
             "inform you that our monthly sessions have come to an end,\n" +
                $"If you could place a block payment before our next session that would be great.";

            messageService.SendSMSMessage(trainer: null, client, SENDER_PHONE_NUMBER!, notificationMessage);

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage,
                reminderType: Enums.NotificationType.ClientBlockCompletionReminder,
                sentThrough: Enums.CommunicationType.SMS);

            if (!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was unsuccessful", Success = false };
            }
            return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was successful", Success = true };
        }
    }
}

