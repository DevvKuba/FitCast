using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class BookingSeriesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService) : BaseAPIController
    {
        /// <summary>
        /// Trainer method creating a new booking series, materializing its initial session slots
        /// </summary>
        [HttpPost("createBookingSeries")]
        public async Task<ActionResult<ApiResponseDto<string>>> CreateBookingSeriesAsync([FromBody] BookingSeriesCreateDto bookingSeriesDetails)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer not found", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(bookingSeriesDetails.ClientId);

            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "client associated to booking series not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());

            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to create a booking series for this client", Success = false });
            }

            await unitOfWork.BookingSeriesRepository.CreateBookingSeriesUnderTrainerAsync(trainer, bookingSeriesDetails);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving booking series: {bookingSeriesDetails.Title}", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = bookingSeriesDetails.Title, Message = $"booking series: {bookingSeriesDetails.Title} successfully created", Success = true });

        }

        /// <summary>
        /// Trainer method changing the start date of an existing booking series
        /// </summary>
        [HttpPut("updateBookingSeriesStartDate")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateBookingSeriesStartDateAsync([FromQuery] int bookingSeriesId, [FromBody] BookingSeriesStartDateUpdateDto updatedStartDate)
        {
            var bookingSeries = await unitOfWork.BookingSeriesRepository.GetTrainerBookingSeriesByIdAsync(currentUserAccessor.GetUserId(), bookingSeriesId);

            if (bookingSeries is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "booking series not found", Success = false });
            }

            unitOfWork.BookingSeriesRepository.UpdateBookingSeriesStartDate(bookingSeries, updatedStartDate);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error updating start date for booking series: {bookingSeries.Title}", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = bookingSeries.Title, Message = $"booking series: {bookingSeries.Title} start date updated to {bookingSeries.StartDate}", Success = true });
        }

        /// <summary>
        /// Trainer method deleting a booking series, along with its materialized session slots
        /// </summary>
        [HttpDelete("deleteBookingSeries")]
        public async Task<ActionResult<ApiResponseDto<bool>>> DeleteBookingSeriesAsync([FromQuery] int bookingSeriesId)
        {
            var bookingSeries = await unitOfWork.BookingSeriesRepository.GetTrainerBookingSeriesByIdAsync(currentUserAccessor.GetUserId(), bookingSeriesId);

            if (bookingSeries is null)
            {
                return NotFound(new ApiResponseDto<bool> { Data = false, Message = "booking series not found", Success = false });
            }

            unitOfWork.BookingSeriesRepository.DeleteBookingSeries(bookingSeries);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<bool> { Data = false, Message = $"error deleting booking series: {bookingSeries.Title}", Success = false });
            }
            return Ok(new ApiResponseDto<bool> { Data = true, Message = $"booking series: {bookingSeries.Title} successfully deleted", Success = true });
        }
    }
}
