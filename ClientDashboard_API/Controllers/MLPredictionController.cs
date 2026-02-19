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
        IWebHostEnvironment environment,
        IRevenueDataExtenderService revenueDataService
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

                return Ok(new ApiResponseDto<PredictionResultDto> { Data = predictionResultData, Message = $"Predicted next month revenue: {prediction:F2}", Success = true });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new ApiResponseDto<PredictionResultDto>{ Data = null, Message = "No trained model found. Please train the model first.", Success = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDto<PredictionResultDto>{ Data = null, Message = $"Prediction failed: {ex.Message}", Success = false });
            }

        }
        /// <summary>
        /// Trains a revenue prediction model for the specified trainer using all available tracked revenue records.
        /// </summary>
        /// <remarks>Returns a bad request response if the specified trainer does not exist or if model
        /// training cannot be performed due to invalid input or state.</remarks>
        /// <param name="trainerId">The unique identifier of the trainer for whom the revenue model will be trained. Must correspond to an
        /// existing trainer.</param>
        /// <returns>An ActionResult containing an ApiResponseDto with the model training metrics if successful; otherwise, an
        /// error response with details about the failure.</returns>
        [HttpPost("trainRevenueModel")]
        public async Task<ActionResult<ApiResponseDto<ModelMetrics>>> TrainRevenueModelAsync([FromQuery] int trainerId)
        {
            try
            {
                var metrics = await trainingService.TrainModelAsync(trainerId);

                return Ok(new ApiResponseDto<ModelMetrics>{ Data = metrics, Message = $"Model trained successfully. R² = {metrics.RSquared:F3}", Success = true });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponseDto<ModelMetrics>{ Data = null, Message = ex.Message, Success = false });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto<ModelMetrics>{ Data = null, Message = ex.Message, Success = false });
            }
        }


        /// <summary>
        /// Alternative approach where if at least 60 records exist for trainer, they are analysed with specific metrics being collected.
        /// Such as baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth, 
        /// which in turn allows for more accurate creation of further revenue records
        /// </summary>
        /// <remarks>Returns a bad request response if the specified trainer does not exist or if model
        /// training cannot be performed due to invalid input or state.</remarks>
        /// <param name="trainerId">The unique identifier of the trainer for whom the revenue model will be trained. Must correspond to an
        /// existing trainer.</param>
        /// <returns>An ActionResult containing an ApiResponseDto with the model training metrics if successful; otherwise, an
        /// error response with details about the failure.</returns>
        [HttpPost("extendAndTrainRevenueModel")]
        public async Task<ActionResult<ApiResponseDto<ModelMetrics>>> ExtendTrainerRevenueRecordsAndTrainRevenueModelAsync([FromQuery] int trainerId)
        {

            // need to check if there is at least 60 records under that trainer to allow the extension
            var trainerElegible = await unitOfWork.TrainerDailyRevenueRepository.CanTrainerExtendRevenueRecordsAsync(trainerId);

            if (!trainerElegible) 
            {
                return BadRequest(new ApiResponseDto<ModelMetrics> { Data = null, Message = "trainer must have at least 60 active records for extension", Success = false });
            }

            try
            {
               var firstRecord = await revenueDataService.ProvideExtensionRecordsForRevenueDataAsync(trainerId);

                // gather metrics through training
                var prediction = await predictionService.PredictNextMonthRevenueAsync(trainerId);

                var predictionResultData = new PredictionResultDto
                {
                    TrainerId = trainerId,
                    PredictedRevenue = prediction,
                    PredictedDate = DateTime.Now,
                };

                // delete all dummy extended data - leaving only the original records
                await unitOfWork.TrainerDailyRevenueRepository.DeleteExtensionRecordsUpToDateAsync(firstRecord);

                if (!await unitOfWork.Complete())
                {
                    return BadRequest(new ApiResponseDto<ModelMetrics> { Data = null, Message = $"Extension records were not successfully removed from trainer with id: {trainerId}", Success = false });
                }

                return Ok(new ApiResponseDto<PredictionResultDto> { Data = predictionResultData, Message = $"Predicted next month revenue: {prediction:F2}", Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDto<ModelMetrics> { Data = null, Message = ex.Message, Success = false });
            }
        }

        [HttpPost("generateDummyData")]
        [AllowAnonymous] // Remove this if you want to keep auth
        public async Task<ActionResult<ApiResponseDto<DummyDataSummaryDto>>> GenerateDummyDataAsync([FromQuery] int trainerId,[FromQuery] int numberOfMonths = 12)
        {
            if (!environment.IsDevelopment())
            {
                return BadRequest(new ApiResponseDto<DummyDataSummaryDto>{ Data = null, Message = "Dummy data generation only allowed in Development environment", Success = false });
            }

            try
            {
                var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);
                if (trainer == null)
                {
                    return NotFound(new ApiResponseDto<DummyDataSummaryDto>{ Data = null, Message = $"Trainer {trainerId} not found", Success = false });
                }

                // Delete existing dummy data for this trainer (optional - clean slate)
                var existingRecords = await unitOfWork.TrainerDailyRevenueRepository
                    .GetAllRevenueRecordsForTrainerAsync(trainerId);

                if (existingRecords.Any())
                {
                    logger.LogWarning("Trainer {TrainerId} already has {Count} records. These will be replaced.",
                        trainerId, existingRecords.Count);
                    await unitOfWork.TrainerDailyRevenueRepository.ResetTrainerDailyRevenueRecordsAsync(trainerId);
                    await unitOfWork.Complete();
                    
                }

                // Generate data based on scenario
                List<TrainerDailyRevenue> dummyData = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

                // Inject into database
                foreach (var record in dummyData)
                {
                    await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDummyReveneRecordAsync(record);
                }

                if (!await unitOfWork.Complete())
                {
                    return BadRequest(new ApiResponseDto<DummyDataSummaryDto>{ Data = null, Message = "Failed to save dummy data", Success = false });
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
                    Scenario = "realistic"
                };

                return Ok(new ApiResponseDto<DummyDataSummaryDto>{ Data = summary, Message = $"Generated {dummyData.Count} days of dummy data for Trainer {trainerId}", Success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate dummy data for Trainer {TrainerId}", trainerId);
                return BadRequest(new ApiResponseDto<DummyDataSummaryDto>{ Data = null, Message = $"Error: {ex.Message}", Success = false });
            }
        }
    }
}
