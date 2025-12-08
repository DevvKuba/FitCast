using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Runtime.CompilerServices;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class PaymentController(IUnitOfWork unitOfWork) : BaseAPIController
    {
        [HttpGet("getAllTrainerPayments")]
        public async Task<ActionResult<ApiResponseDto<List<Payment>>>> GetTrainerPaymentsAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);
            if (trainer == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }
            var trainerPayments = await unitOfWork.PaymentRepository.GetAllPaymentsForTrainerAsync(trainer);

            return  Ok(new ApiResponseDto<List<Payment>> { Data = trainerPayments, Message = $"Successfully gathered trainer: {trainer.FirstName}'s payments", Success = true });

        }

        [HttpPut("updateExistingPayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdatePaymentInformationAsync([FromBody] PaymentUpdateRequestDto paymentRequestInfo)
        {
            var payment = await unitOfWork.PaymentRepository.GetPaymentWithClientByIdAsync(paymentRequestInfo.Id);

            if (payment == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"payment does not exist", Success = false });
            }

            unitOfWork.PaymentRepository.UpdatePaymentDetails(payment, paymentRequestInfo);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payment", Success = false });
            }
            // we are returning payment within trainer or client
            return Ok(new ApiResponseDto<string> { Data = payment.Id.ToString(), Message = $"Payment for client: {payment.Client!.FirstName} has been updated successfully", Success = true });


        }

        [HttpPost("addPayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewTrainerPaymentAsync([FromBody] PaymentAddDto paymentInfo)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(paymentInfo.TrainerId);
            if (trainer == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(paymentInfo.ClientId);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the id:{paymentInfo.ClientId} found", Success = false });
            }

            await unitOfWork.PaymentRepository.AddNewPaymentAsync(trainer, client, paymentInfo.NumberOfSessions, paymentInfo.Amount, DateOnly.Parse(paymentInfo.PaymentDate), paymentInfo.Confirmed);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payments after deletion", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"Payment for trainer: {trainer.FirstName} and their client: {client.FirstName} has been added successfully", Success = true });

        }

        [HttpDelete("deletePayment")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteTrainerPaymentAsync([FromQuery] int paymentId)
        {
            var payment = await unitOfWork.PaymentRepository.GetPaymentWithRelatedEntitiesById(paymentId);

            if (payment == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"payment with id: {paymentId} does not exist", Success = false });
            }

            unitOfWork.PaymentRepository.DeletePayment(payment);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving payments after deletion", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = payment.Id.ToString(), Message = $"Payment for trainer: {payment.Trainer.FirstName} and their client: {payment.Client!.FirstName} has been deleted successfully", Success = true });
        }

        [HttpDelete("filterClientPayments")]
        public async Task<ActionResult<ApiResponseDto<string>>> FilterClientPaymentsAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if(trainer == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"trainer was not found", Success = false });
            }

            await unitOfWork.PaymentRepository.FilterOldClientPaymentsAsync(trainer);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error filtering old trainer clients", Success = false });
            }
            return Ok(new ApiResponseDto<string> {Data = trainer.FirstName, Message = "successfully filtered " })

        }
    }
}
