using ClientDashboard_API.Authorization;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Runtime.CompilerServices;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class PaymentController(
        IUnitOfWork unitOfWork,
        IAuthorizationService authorizationService,
        ICurrentUserAccessor currentUserAccessor
        ) : BaseAPIController
    {

        [Authorize(Roles = "Client")]
        [HttpGet("getClientSpecificPayments")]
        public async Task<ActionResult<ApiResponseDto<List<Payment>>>> GetClientPaymentsAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "client does not exist", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to view this client's payments", Success = false });
            }

            var clientPayments = await unitOfWork.PaymentRepository.GetAllClientSpecificPaymentsAsync(client);

            return Ok(new ApiResponseDto<List<Payment>> { Data = clientPayments, Message = $"Successfully gathered client: {client.FirstName}'s payments", Success = true });

        }

        [Authorize(Roles = "Trainer")]
        [HttpGet("getAllTrainerPayments")]
        public async Task<ActionResult<ApiResponseDto<List<Payment>>>> GetTrainerPaymentsAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());
            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }
            var trainerPayments = await unitOfWork.PaymentRepository.GetAllPaymentsForTrainerAsync(trainer);

            return  Ok(new ApiResponseDto<List<Payment>> { Data = trainerPayments, Message = $"Successfully gathered trainer: {trainer.FirstName}'s payments", Success = true });

        }

        [Authorize(Roles = "Trainer")]
        [HttpPut("updateExistingPayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdatePaymentInformationAsync([FromBody] PaymentUpdateRequestDto paymentRequestInfo)
        {
            var payment = await unitOfWork.PaymentRepository.GetPaymentWithClientByIdAsync(paymentRequestInfo.Id);

            if (payment is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"payment does not exist", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, payment, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to update this payment", Success = false });
            }

            if (payment.Client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "client does not exist", Success = false });
            }

            unitOfWork.PaymentRepository.UpdatePaymentDetails(payment, paymentRequestInfo);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payment", Success = false });
            }
            // we are returning payment within trainer or client
            return Ok(new ApiResponseDto<string> { Data = payment.Id.ToString(), Message = $"Payment for client: {payment.Client!.FirstName} has been updated successfully", Success = true });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPut("updateAllPaymentVisibilityStatusses")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateTrainersPaymentVisibilityStatussesAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());

            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"trainer does not exist", Success = false });
            }

            await unitOfWork.PaymentRepository.UpdateAllTrainerPaymentsToVisibleStatusAsync(trainer);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payment statusses", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"Updated all payment to visible, for trainer with id: {trainer.Id}", Success = true });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost("addPayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewTrainerPaymentAsync([FromBody] PaymentAddDto paymentInfo)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());
            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(paymentInfo.ClientId);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the id:{paymentInfo.ClientId} found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to add a payment for this client", Success = false });
            }

            await unitOfWork.PaymentRepository.AddNewPaymentAsync(trainer, client, paymentInfo.NumberOfSessions, paymentInfo.Amount, DateOnly.Parse(paymentInfo.PaymentDate), paymentInfo.Confirmed);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payments after deletion", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"Payment for {client.FirstName}  of {paymentInfo.Amount}{trainer.DefaultCurrency} has been added successfully", Success = true });

        }

        [Authorize(Roles = "Trainer")]
        [HttpPut("deletePayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteTrainerPaymentAsync([FromQuery] int paymentId)
        {
            var payment = await unitOfWork.PaymentRepository.GetPaymentWithRelatedEntitiesById(paymentId);

            if (payment is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"payment with id: {paymentId} does not exist", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, payment, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to remove this payment", Success = false });
            }

            unitOfWork.PaymentRepository.DisablePaymentVisibility(payment);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payments after deletion", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = payment.Id.ToString(), Message = $"Payment for client: {payment.Client!.FirstName} on {payment.PaymentDate} has been deleted successfully", Success = true });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPut("filterClientPayments")]
        public async Task<ActionResult<ApiResponseDto<int?>>> FilterClientPaymentsAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(currentUserAccessor.GetUserId());

            if(trainer is null)
            {
                return NotFound(new ApiResponseDto<int?> { Data = null, Message = $"Trainer was not found", Success = false });
            }

            var trainerDeletedClients = await unitOfWork.TrainerRepository.GatherDeletedTrainerClientsByTrainerIdAsync(trainer.Id);

            var filteredPaymentCount = await unitOfWork.PaymentRepository.FilterOldClientPaymentsAsync(trainerDeletedClients);

            if(filteredPaymentCount == 0)
            {
                return Ok(new ApiResponseDto<int?> { Data = 0, Message = $"No old client payments present to filter", Success = true });
            }

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<int?> { Data = null, Message = "Error filtering old trainer clients", Success = false });
            }
            return Ok(new ApiResponseDto<int?> { Data = filteredPaymentCount, Message = $"Successfully filtered: {filteredPaymentCount} old client payment", Success = true });

        }
    }
}
