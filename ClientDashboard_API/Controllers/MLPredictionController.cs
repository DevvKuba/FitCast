using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz.Logging;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class MLPredictionController(
        IMLPredictionService predictionService,
        IMLModelTrainingService trainingService,
        ILogger<MLPredictionController> logger,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment
        ) : BaseAPIController
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
            try
            {
                var metrics = await trainingService.TrainModelAsync(trainerId);

                return Ok(new ApiResponseDto<ModelMetrics>
                {
                    Data = metrics,
                    Message = $"Model trained successfully. R² = {metrics.RSquared:F3}",
                    Success = true
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponseDto<ModelMetrics>
                {
                    Data = null,
                    Message = ex.Message,
                    Success = false
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto<ModelMetrics>
                {
                    Data = null,
                    Message = ex.Message,
                    Success = false
                });
            }
        }

        [HttpPost("generateDummyData")]
        [AllowAnonymous] // Remove this if you want to keep auth
        public async Task<ActionResult<ApiResponseDto<DummyDataSummaryDto>>> GenerateDummyDataAsync(
        [FromQuery] int trainerId,
        [FromQuery] int numberOfMonths = 6,
        [FromQuery] string scenario = "realistic")
        {
            if (!environment.IsDevelopment())
            {
                return BadRequest(new ApiResponseDto<DummyDataSummaryDto>
                {
                    Data = null,
                    Message = "Dummy data generation only allowed in Development environment",
                    Success = false
                });
            }

            try
            {
                // Verify trainer exists
                var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);
                if (trainer == null)
                {
                    return NotFound(new ApiResponseDto<DummyDataSummaryDto>
                    {
                        Data = null,
                        Message = $"Trainer {trainerId} not found",
                        Success = false
                    });
                }

                // Delete existing dummy data for this trainer (optional - clean slate)
                var existingRecords = await unitOfWork.TrainerDailyRevenueRepository
                    .GetAllRevenueRecordsForTrainerAsync(trainerId);

                if (existingRecords.Any())
                {
                    logger.LogWarning("Trainer {TrainerId} already has {Count} records. These will be replaced.",
                        trainerId, existingRecords.Count);
                    await unitOfWork.TrainerDailyRevenueRepository.ResetTrainerDailyRevenueRecords(trainerId);
                    
                }

                // Generate data based on scenario
                List<TrainerDailyRevenue> dummyData = scenario.ToLower() switch
                {
                    "highgrowth" => DummyDataGenerator.GenerateHighGrowthScenario(trainerId, numberOfMonths),
                    "flat" => DummyDataGenerator.GenerateFlatScenario(trainerId, numberOfMonths),
                    _ => DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths)
                };

                // Inject into database
                foreach (var record in dummyData)
                {
                    await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDummyReveneRecordAsync(record);
                }

                if (!await unitOfWork.Complete())
                {
                    return BadRequest(new ApiResponseDto<DummyDataSummaryDto>
                    {
                        Data = null,
                        Message = "Failed to save dummy data",
                        Success = false
                    });
                }

                // Calculate summary statistics
                var summary = new DummyDataSummaryDto
                {
                    TrainerId = trainerId,
                    TrainerName = trainer.FirstName,
                    RecordsGenerated = dummyData.Count,
                    DateRange = $"{dummyData.First().AsOfDate} to {dummyData.Last().AsOfDate}",
                    TotalRevenue = dummyData.Sum(r => r.RevenueToday),
                    AverageMonthlyRevenue = dummyData.GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                        .Average(g => g.Sum(r => r.RevenueToday)),
                    StartingActiveClients = dummyData.First().ActiveClients,
                    EndingActiveClients = dummyData.Last().ActiveClients,
                    Scenario = scenario
                };

                return Ok(new ApiResponseDto<DummyDataSummaryDto>
                {
                    Data = summary,
                    Message = $"Generated {dummyData.Count} days of dummy data for Trainer {trainerId}",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate dummy data for Trainer {TrainerId}", trainerId);
                return BadRequest(new ApiResponseDto<DummyDataSummaryDto>
                {
                    Data = null,
                    Message = $"Error: {ex.Message}",
                    Success = false
                });
            }
        }
    }
}
