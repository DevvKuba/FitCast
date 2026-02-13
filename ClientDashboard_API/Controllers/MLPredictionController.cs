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

        }

        [HttpPost("trainRevenueModel")]
        public async Task<ActionResult<ApiResponseDto<ModelMetrics>>> TrainRevenueModelAsync([FromQuery] int trainerId)
        {

        }
    }
}
