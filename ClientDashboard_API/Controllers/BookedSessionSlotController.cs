using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class BookedSessionSlotController(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserAccessor currentUserAccessor) : BaseAPIController
    {
        /// <summary>
        /// Trainer method adding a standalone booked session slot, not tied to a booking series
        /// </summary>
        [HttpPost("addBookedSessionSlot")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddBookedSessionSlotAsync([FromBody] BookedSessionSlotAddDto bookedSessionSlotDetails)
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
        public async Task<ActionResult<ApiResponseDto<bool>>> DeleteBookedSessionSlotAsync([FromQuery] int bookedSessionSlotId)
        {
            
        }
    }
}
