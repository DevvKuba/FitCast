using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyInvisiblePaymentCleanup(IUnitOfWork unitOfWork, ILogger<DailyInvisiblePaymentCleanup> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyInvisiblePaymentCleanup job STARTED at {StartTime} UTC", DateTime.UtcNow);

            var deletedPaymentCount = 0;

            try
            {
                var invisiblePayments = await unitOfWork.PaymentRepository.GetAllInvisiblePaymentsAsync();

                if (invisiblePayments.Count == 0)
                {
                    logger.LogInformation("No invisible payments found to process at {Time} UTC", DateTime.UtcNow);
                    return;
                }

                var cutoffDate = DateTime.UtcNow.AddMonths(-3);
                logger.LogInformation("Found {InvisiblePaymentsCount} invisible payments. Deleting records on or before {CutoffDate}",
                    invisiblePayments.Count, DateOnly.FromDateTime(cutoffDate));

                foreach (var payment in invisiblePayments)
                {
                    if (payment.PaymentDate <= DateOnly.FromDateTime(cutoffDate))
                    {
                        unitOfWork.PaymentRepository.DeletePayment(payment);
                        deletedPaymentCount++;
                    }
                }

                if (deletedPaymentCount == 0)
                {
                    logger.LogInformation("No invisible payments were old enough for deletion at {Time} UTC", DateTime.UtcNow);
                    return;
                }

                await unitOfWork.Complete();

                logger.LogInformation("DailyInvisiblePaymentCleanup job FINISHED at {EndTime} UTC. Deleted {DeletedPayments} payments",
                    DateTime.UtcNow, deletedPaymentCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DailyInvisiblePaymentCleanup job FAILED at {Time} UTC after deleting {DeletedPayments} pending records",
                    DateTime.UtcNow, deletedPaymentCount);
                throw;
            }
        }
    }
}
