using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
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

            var reminderType = Enums.NotificationType.TrainerBlockCompletionReminder;

            // retrieve the notificaiton message based on the reminder type

            var notificationMessage = NotificationMessageHelper.GetMessage(reminderType, trainer, client);

            // IMP uncomment for testing with Twillio credits
            //if (trainer.NotificationsEnabled && trainer.PhoneNumber is not null)
            //{
            //messageService.SendSMSMessage(trainer, client: null, SENDER_PHONE_NUMBER!, notificationMessage);
            //}

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage, reminderType,
                sentThrough: Enums.CommunicationType.Sms);

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
            var reminderType = Enums.NotificationType.ClientBlockCompletionReminder;

            var notificationMessage = NotificationMessageHelper.GetMessage(reminderType, trainer, client);

            // IMP uncomment for testing with Twillio credits
            //if (client.NotificationsEnabled && client.PhoneNumber is not null)
            //{
            //messageService.SendSMSMessage(trainer, client: null, SENDER_PHONE_NUMBER!, notificationMessage);
            //}

            await unitOfWork.NotificationRepository.AddNotificationAsync(trainerId, clientId, notificationMessage,
                reminderType: Enums.NotificationType.ClientBlockCompletionReminder,
                sentThrough: Enums.CommunicationType.Sms);

            if (!await unitOfWork.Complete())
            {
                return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was unsuccessful", Success = false };
            }
            return new ApiResponseDto<string> { Data = null, Message = $"Saving notification message: {notificationMessage} was successful", Success = true };
        }
    }
}

