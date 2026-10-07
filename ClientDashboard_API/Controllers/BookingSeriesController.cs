using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class BookingSeriesController(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserAccessor currentUserAccessor) : BaseAPIController
    {
        /// <summary>
        /// Trainer method creating a new booking series, materializing its initial session slots
        /// </summary>
        [HttpPost("createBookingSeries")]
        public async Task<ActionResult<ApiResponseDto<string>>> CreateBookingSeriesAsync([FromBody] BookingSeriesCreateDto bookingSeriesDetails)
        {

        }

        /// <summary>
        /// Trainer method changing the start date of an existing booking series
        /// </summary>
        [HttpPut("updateBookingSeriesStartDate")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateBookingSeriesStartDateAsync([FromQuery] int bookingSeriesId, [FromBody] BookingSeriesStartDateUpdateDto updatedStartDate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Trainer method deleting a booking series, along with its materialized session slots
        /// </summary>
        [HttpDelete("deleteBookingSeries")]
        public async Task<ActionResult<ApiResponseDto<bool>>> DeleteBookingSeriesAsync([FromQuery] int bookingSeriesId)
        {
            throw new NotImplementedException();
        }
    }
}
