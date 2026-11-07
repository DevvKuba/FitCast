using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class BookedSessionSlotController(
        IUnitOfWork unitOfWork,
        IMapper mapper, 
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService) : BaseAPIController
    {
        private const int MaxRangeDays = 42; // a 6 week month grid

        /// <summary>
        /// Trainer method retrieving their booked session slots starting within the given UTC window (the calendar's visible range)
        /// </summary>
        [HttpGet("getTrainerBookedSessionSlots")]
        public async Task<ActionResult<ApiResponseDto<List<BookedSessionSlotViewDto>>>> GetBookingSessionSlotsForTrainerAsync([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc)
        {
            // a trailing Z binds as UTC, without one the server would have to guess the zone
            if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc)
            {
                return BadRequest(new ApiResponseDto<List<BookedSessionSlotViewDto>> { Data = null, Message = "Range must be provided in UTC", Success = false });
            }

            if (toUtc <= fromUtc)
            {
                return BadRequest(new ApiResponseDto<List<BookedSessionSlotViewDto>> { Data = null, Message = "End of range must be after the start", Success = false });
            }

            if ((toUtc - fromUtc).TotalDays > MaxRangeDays)
            {
                return BadRequest(new ApiResponseDto<List<BookedSessionSlotViewDto>> { Data = null, Message = $"Range cannot be longer than {MaxRangeDays} days", Success = false });
            }

            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<List<BookedSessionSlotViewDto>> { Data = null, Message = "Trainer not found", Success = false });
            }

            var bookedSessionSlots = await unitOfWork.BookedSessionSlotRepository.GetTrainerBookedSessionSlotsInRangeAsync(trainer.Id, fromUtc, toUtc);

            return Ok(new ApiResponseDto<List<BookedSessionSlotViewDto>> { Data = bookedSessionSlots, Message = $"{bookedSessionSlots.Count} booked session slots retrieved for {trainer.FirstName}", Success = true });
        }

        /// <summary>
        /// Trainer method updating the details of a single booked session slot
        /// </summary>
        [HttpPut("updateBookedSessionSlot")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateBookingSessionSlotForTrainerAsync([FromQuery] int bookedSessionSlotId, [FromBody] BookedSessionSlotDto newBookedSessionSlot)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Trainer not found", Success = false });
            }

            var bookedSessionSlot = await unitOfWork.BookedSessionSlotRepository.GetTrainerBookedSessionSlotByIdAsync(trainer.Id, bookedSessionSlotId);

            if (bookedSessionSlot is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Booked session slot not found", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(newBookedSessionSlot.ClientId);

            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Client associated to booked session not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());

            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to book sessions for this client", Success = false });
            }

            unitOfWork.BookedSessionSlotRepository.UpdateBookedSessionSlot(bookedSessionSlot, newBookedSessionSlot);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Error updating booked session slot for {client.FirstName}", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"Booked session slot updated to {newBookedSessionSlot.StartDateTime} for {client.FirstName}", Success = true });
        }

        /// <summary>
        /// Trainer method adding a standalone booked session slot, not tied to a booking series
        /// </summary>
        [HttpPost("addBookedSessionSlot")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddBookedSessionSlotAsync([FromBody] BookedSessionSlotDto bookedSessionSlotDetails)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Trainer not found", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(bookedSessionSlotDetails.ClientId);

            if(client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Client associated to booked session not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());

            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to book sessions for this client", Success = false });
            }

            await unitOfWork.BookedSessionSlotRepository.AddBookingSlotForTrainerAsync(trainer, bookedSessionSlotDetails);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Error saving booked session slot for {client.FirstName}", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"Booked session slot at {bookedSessionSlotDetails.StartDateTime} created for {client.FirstName}", Success = true });
        }

        /// <summary>
        /// Trainer method deleting a single booked session slot
        /// </summary>
        [HttpDelete("deleteBookedSessionSlot")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteBookedSessionSlotAsync([FromQuery] int bookedSessionSlotId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Trainer not found", Success = false });
            }

            var bookedSessionSlot = await unitOfWork.BookedSessionSlotRepository.GetTrainerBookedSessionSlotByIdAsync(trainer.Id, bookedSessionSlotId);

            if(bookedSessionSlot is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Booked session slot not found", Success = false });
            }

            unitOfWork.BookedSessionSlotRepository.RemoveBookedSessionSlot(bookedSessionSlot);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Error removing booked session slot", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"Booked session slot removed", Success = true });
        }
    }
}
