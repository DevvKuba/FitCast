using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyMaterializationJob(
        IUnitOfWork unitOfWork,
        IBookingMaterializationService materializationService,
        ILogger<DailyMaterializationJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyMaterializationJob job STARTED at {StartTime} UTC", DateTime.UtcNow);

            var advancedSeriesCount = 0;
            var failedSeriesCount = 0;

            try
            {
                var bookingSeries = await unitOfWork.BookingSeriesRepository.GetAllBookingSeriesAsync();

                if (bookingSeries.Count == 0)
                {
                    logger.LogInformation("No booking series found to materialize at {Time} UTC", DateTime.UtcNow);
                    return;
                }

                foreach (var series in bookingSeries)
                {
                    var previousMaterializedUntil = series.MaterializedUntil;

                    // one bad series (misaligned watermark, unknown time zone) shouldn't stop the rest.
                    // the service throws before staging any slots, so nothing partial is left behind
                    try
                    {
                        await materializationService.MaterializeBookingSeriesAsync(series);
                    }
                    catch (Exception ex)
                    {
                        failedSeriesCount++;
                        logger.LogError(ex, "Failed to materialize booking series {BookingSeriesId} for trainer {TrainerId}",
                            series.Id, series.TrainerId);
                        continue;
                    }

                    if (series.MaterializedUntil != previousMaterializedUntil)
                    {
                        advancedSeriesCount++;
                        logger.LogInformation("Booking series {BookingSeriesId} for trainer {TrainerId} materialized from {PreviousMaterializedUntil} to {MaterializedUntil}",
                            series.Id, series.TrainerId, previousMaterializedUntil, series.MaterializedUntil);
                    }
                }

                await unitOfWork.Complete();

                logger.LogInformation("DailyMaterializationJob job FINISHED at {EndTime} UTC. Checked {SeriesCount} series, advanced {AdvancedSeriesCount}, failed {FailedSeriesCount}",
                    DateTime.UtcNow, bookingSeries.Count, advancedSeriesCount, failedSeriesCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DailyMaterializationJob job FAILED at {Time} UTC after advancing {AdvancedSeriesCount} series",
                    DateTime.UtcNow, advancedSeriesCount);
                throw;
            }
        }
    }
}
