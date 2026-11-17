using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.ML.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using Twilio.Rest.Trunking.V1;
using Twilio.TwiML.Voice;
using Twilio.Types;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ITrainerFullMonthAnalyticsService fullMonthAnalyticsService,
        ITrainerCurrentMonthAnalyticsService currentMonthAnalyticsService,
        ICurrentUserAccessor currentUserAccessor,
        ITrainerDependencyRemovalHelper trainerDependencyRemovalHelper
        ) : BaseAPIController
    {
        /// <summary>
        /// Trainer method allowing for the retrieval of a specific Trainer by id
        /// </summary>
        [HttpGet("retrieveTrainerById")]
        public async Task<ActionResult<ApiResponseDto<Trainer>>> RetrieveTrainerByIdAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<Trainer> { Data = null, Message = "trainer does not exist", Success = false });
            }

            return Ok(new ApiResponseDto<Trainer> { Data = trainer, Message = $"trainer: {trainer.FirstName} successfully retrieved", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of client under them
        /// </summary>
        [HttpPut("updateTrainerProfileDetails")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateTrainerProfileAsync([FromBody] TrainerUpdateDto updatedTrainerProfile)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<Trainer> { Data = null, Message = "trainer does not exist", Success = false });
            }

            if (!SupportedTimeZones.IsSupported(updatedTrainerProfile.TimeZoneId))
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "A supported time zone must be provided", Success = false });
            }

            // captured before mapping, as the mapper overwrites the trainer's TimeZoneId
            var previousTimeZoneId = trainer.TimeZoneId;

            unitOfWork.TrainerRepository.UpdateTrainerProfileDetailsAsync(trainer, updatedTrainerProfile);

            if (previousTimeZoneId != updatedTrainerProfile.TimeZoneId)
            {
                // series store local clock time so need no change, already booked slots are UTC and are shifted to keep their clock time
                var oldZone = TimeZoneInfo.FindSystemTimeZoneById(previousTimeZoneId);
                var newZone = TimeZoneInfo.FindSystemTimeZoneById(updatedTrainerProfile.TimeZoneId);

                await unitOfWork.BookedSessionSlotRepository.UpdateFutureScheduledSlotsToTimeZoneAsync(trainer.Id, oldZone, newZone, DateTime.UtcNow);
            }

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving trainer profile", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName} has had their profile updated successfully", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of client under them
        /// </summary>
        [HttpPut("assignClient")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateClientAssignmentAsync([FromQuery] int clientId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "client does not exist", Success = false });
            }

            unitOfWork.TrainerRepository.AssignClient(trainer, client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving date when updating clients trainer", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = client.FirstName, Message = $"client: {client.FirstName} is now under trainer: {trainer.FirstName}", Success = true });
        }

        /// <summary>
        /// Trainer method updating a Workout Retrieval Api Key, along with the toggle status of AutoRetrival
        /// </summary>
        [HttpPut("updateTrainerPaymentSetting")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateTrainerPaymentSettingAsync([FromQuery] bool enabled)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            if(trainer.DefaultCurrency == null || trainer.AverageSessionPrice == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Must have set both default currency and average session price in Profile, before proceeding", Success = false });
            }

            unitOfWork.TrainerRepository.UpdateTrainerPaymentSettingAsync(trainer, enabled);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving {trainer.FirstName}'s new payment setting status", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName}'s payment setting status successfully set up", Success = true });

        }

        //summary>
        /// Trainer method to retrieve a trainer's Auto Payment Setting status
        /// </summary>
        [HttpGet("getAutoPaymentSettingStatus")]
        public async Task<ActionResult<ApiResponseDto<bool>>> GetAutoPaymentSettingStatusAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            return Ok(new ApiResponseDto<bool> { Data = trainer.AutoPaymentSetting, Message = $"trainer: {trainer.FirstName}'s auto retrieval status enquired successfully", Success = true });
        }

        [HttpGet("getTrainerCurrentMonthsAnalytics")]
        public async Task<ActionResult<ApiResponseDto<CurrentMonthTrainerAnalyticsDto>>> GetCurrentMonthAnalytics()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<CurrentMonthTrainerAnalyticsDto> {Data = null, Message = "trainer does not exist", Success = false });
            }

            var currentMonthsRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetCurrentMonthsRevenueRecordsAsync(trainer.Id);

            if (currentMonthsRevenueRecords.Count == 0 || currentMonthsRevenueRecords == null)
            {
                return Ok(new ApiResponseDto<CurrentMonthTrainerAnalyticsDto> {
                    Data = new CurrentMonthTrainerAnalyticsDto
                    {
                        BaseClients = 0,
                        MonthlyClientSessions = 0,
                        TotalRevenue = 0,
                        TotalWorktimeMinutes = 0,
                        RevenuePerWorkingDay = 0,
                        WeeklySessionsCounts = []
                    }, 
                    Message = "Clean state returning, no revenue records for this month", 
                    Success = true 
                });
            }

            var currentMonthAnalytics = currentMonthAnalyticsService.GetCurrentMonthsAnalyticMetrics(currentMonthsRevenueRecords);

            return Ok(new ApiResponseDto<CurrentMonthTrainerAnalyticsDto> { Data = currentMonthAnalytics, Message = "Successfully retrieved the current month's analytics", Success = true });
        }

        [HttpGet("getTrainerSpecificMonthAnalytics")]
        public async Task<ActionResult<ApiResponseDto<CompleteMonthTrainerAnalyticsDto>>> GetSpecificMonthsAnalytics(int month, int year)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer == null) return NotFound(new ApiResponseDto<CompleteMonthTrainerAnalyticsDto> { Data = null, Message = "trainer not found", Success = false });

            var monthRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetSpecificFullMonthRecordsAsync(trainer.Id, month, year);

            if (!unitOfWork.TrainerDailyRevenueRepository.IsFullMonthPresent(monthRevenueRecords))
            {
                return BadRequest(new ApiResponseDto<CompleteMonthTrainerAnalyticsDto> { Data = null, Message = "Full month of data not retrieved", Success = false });
            }

            var monthAnalytics = fullMonthAnalyticsService.GetAllAnalyticMetrics(monthRevenueRecords);

            return Ok(new ApiResponseDto<CompleteMonthTrainerAnalyticsDto> { Data = monthAnalytics, Message = $"Successfully retrieved revenue records for {month}/{year}", Success = true });
        }

        [HttpDelete("deleteTrainer")]
        public async Task<ActionResult<ApiResponseDto<bool>>> DeleteTrainerAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<bool> { Data = false, Message = "Account cannot be found for deletion", Success = false });
            }

            await trainerDependencyRemovalHelper.RemoveAllTrainerAssociatedDependenciesAsync(trainer);

            unitOfWork.TrainerRepository.DeleteTrainer(trainer);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<bool> { Data = false, Message = $"Problem deleting account", Success = false });
            }
            return Ok(new ApiResponseDto<bool> { Data = true, Message = $"Account successfully deleted", Success = true });
        }

    }
}
