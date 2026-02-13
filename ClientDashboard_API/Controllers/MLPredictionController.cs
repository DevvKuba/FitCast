using ClientDashboard_API.DTOs;
using ClientDashboard_API.Enums;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class MLPredictionController(IMLPredictionService predictionService, IMLModelTrainingService trainingService) : BaseAPIController
    {
        [HttpGet("predictMyRevenue")]
        public async Task<ActionResult<ApiResponseDto<PredictionResultDto>>> PredictMyRevenueAsync([FromQuery] int trainerId)
        {
            try
            {
                var prediction = await predictionService.PredictNextMonthRevenueAsync(trainerId);

                var predictionResultData = new PredictionResultDto
                {
                    TrainerId = trainerId,
                    PredictedRevenue = prediction,
                    PredictedDate = DateTime.Now,
                };

                return Ok(new ApiResponseDto<PredictionResultDto> 
                { 
                   Data = predictionResultData,
                   Message = $"Predicted next month revenue: {prediction:F2}",
                   Success = true 
                });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new ApiResponseDto<PredictionResultDto>
                {
                    Data = null,
                    Message = "No trained model found. Please train the model first.",
                    Success = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDto<PredictionResultDto>
                {
                    Data = null,
                    Message = $"Prediction failed: {ex.Message}",
                    Success = false
                });
            }

        }

        [HttpPost("trainRevenueModel")]
        public async Task<ActionResult<ApiResponseDto<ModelMetrics>>> TrainRevenueModelAsync([FromQuery] int trainerId)
        {

        }
    }
}
