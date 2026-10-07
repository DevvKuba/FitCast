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
        public async Task<ActionResult<ApiResponseDto<int>>> AddBookedSessionSlotAsync([FromBody] BookedSessionSlotAddDto bookedSessionSlotDetails)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Trainer method deleting a single booked session slot
        /// </summary>
        [HttpDelete("deleteBookedSessionSlot")]
        public async Task<ActionResult<ApiResponseDto<bool>>> DeleteBookedSessionSlotAsync([FromQuery] int bookedSessionSlotId)
        {
            throw new NotImplementedException();
        }
    }
}
