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
        [HttpGet("trainAndPredictRevenue")]
        public async Task<ActionResult<ApiResponseDto<PredictionResultDto>>> TrainModelAndPredictRevenueAsync([FromQuery] int trainerId)
        {
            try
            {
                var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

                var allRecords = await unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

                var fullMonthRecords = unitOfWork.TrainerDailyRevenueRepository.GetRecordsForFullMonths(allRecords);

                int monthsOfData = unitOfWork.TrainerDailyRevenueRepository.GetAllMonthCountsFromData(fullMonthRecords);

                var confidence = PredictionConfidenceHelper.DetermineConfidenceLevel(monthsOfData);

                // prediction doesn't go through if there is insufficient data
                if(confidence == ML.Enums.PredictionConfidence.Insufficient)
                {
                    return BadRequest(new ApiResponseDto<PredictionResultDto> {Data = null, Message = $"Need at least 2 months of data to make predictions. You have {monthsOfData} month(s).", Success = false});
                }

                var metrics = await trainingService.TrainModelAsync(trainerId);
                
                var prediction = await predictionService.PredictNextMonthRevenueAsync(trainerId);

                var (lowerBound, upperBound) = PredictionConfidenceHelper.CalculatePredictionRange(prediction, metrics.MeanAbsoluteError, confidence);

                var monthPredictedFor = DateTime.UtcNow.AddMonths(1).ToString("MMMM");

                var predictionResultData = new PredictionResultDto
                {
                    TrainerId = trainerId,
                    PredictedRevenue = prediction,
                    LowerBound = (float?)Math.Round(lowerBound, 0),
                    UpperBound = (float?)Math.Round(upperBound, 0),
                    Currency = trainer?.DefaultCurrency ?? null,
                    PredictedDate = DateTime.Now,
                    Confidence = confidence.ToString(),
                    RSquared = metrics.RSquared,
                    MonthsOfData = monthsOfData,
                    Message = PredictionConfidenceHelper.GetConfidenceMessage(confidence, metrics.RSquared)
                };

                return Ok(new ApiResponseDto<PredictionResultDto> { Data = predictionResultData, Message = $"Prediction of {prediction:F2} {trainer?.DefaultCurrency} made for the month of {monthPredictedFor} with {confidence} confidence", Success = true });
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
        //[HttpPost("extendAndTrainRevenueModel")]
        //public async Task<ActionResult<ApiResponseDto<ModelMetrics>>> ExtendTrainerRevenueRecordsAndTrainRevenueModelAsync([FromQuery] int trainerId)
        //{

        //    // need to check if there is at least 60 records under that trainer to allow the extension
        //    var trainerElegible = await unitOfWork.TrainerDailyRevenueRepository.CanTrainerExtendRevenueRecordsAsync(trainerId);

        //    if (!trainerElegible) 
        //    {
        //        return BadRequest(new ApiResponseDto<ModelMetrics> { Data = null, Message = "trainer must have at least 60 active records for extension", Success = false });
        //    }

        //    TrainerDailyRevenue? firstRealRecord = null;

        //    try
        //    {
        //        var extendedRevenueRecords = await revenueDataService.ProvideExtensionRecordsForRevenueDataAsync(trainerId);

        //        firstRealRecord = extendedRevenueRecords.Last();

        //        // can maybe abstract
        //        foreach(var record in extendedRevenueRecords)
        //        {
        //            await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDummyReveneRecordAsync(record);
        //        }
        //        await unitOfWork.Complete();

        //        // train new temporary model
        //        var metrics = await trainingService.TrainModelAsync(trainerId);

        //        var prediction = await predictionService.PredictNextMonthRevenueAsync(trainerId);

        //        var predictionResultData = new PredictionResultDto
        //        {
        //            TrainerId = trainerId,
        //            PredictedRevenue = prediction,
        //            PredictedDate = DateTime.Now,
        //        };

        //        // may not need complete here instead return adequate response depending on r squard value returned through metrics
        //        if(metrics.RSquared < 0.8)
        //        {
        //            return BadRequest(new ApiResponseDto<PredictionResultDto> { Data = predictionResultData, Message = $"Predicted next month revenue: {prediction:F2}, with insufficient R squard coefficient", Success = false });
        //        }
        //        else
        //        {
        //            return Ok(new ApiResponseDto<PredictionResultDto> { Data = predictionResultData, Message = $"Predicted next month revenue: {prediction:F2}, with sufficient R squard coefficient", Success = true });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ApiResponseDto<PredictionResultDto> { Data = null, Message = ex.Message, Success = false });
        //    }
        //    finally
        //    {
        //        // delete all dummy extended data - leaving only the original records
        //        if (firstRealRecord != null)
        //        {
        //            // TODO check if correct
        //            await unitOfWork.TrainerDailyRevenueRepository.DeleteExtensionRecordsUpToDateAsync(firstRealRecord);
        //            await unitOfWork.Complete();
        //        }

        //        // delete temporary model 
        //        var tempModelPath = Path.Combine(environment.ContentRootPath, "ML", "TrainedModels", $"trainer_{trainerId}_revenue_model_TEMP.zip");
        //        if (System.IO.File.Exists( tempModelPath))
        //        {
        //            System.IO.File.Delete(tempModelPath);
        //            logger.LogInformation("Deleted temporary model for Trainer {TrainerId}", trainerId);
        //        }
        //    }
        //}

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
