using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;

namespace ClientDashboard_API.Services
{
    public class BookingMaterializationService(IUnitOfWork unitOfWork) : IBookingMaterializationService
    {
        public async Task MaterializeBookingSeriesAsync(BookingSeries series)
        {
            if(!Enum.IsDefined(series.Recurrence))
            {
                throw new InvalidOperationException("Reocurrance not set to valid value");
            }

            var offset = series.MaterializedUntil.DayNumber - series.StartDate.DayNumber;
            var interval = (int)series.Recurrence;

            if (offset < -interval || offset % interval != 0) // checking its the same weekday & at most one step behind materialization
            {
                throw new InvalidOperationException($"Booking series {series.Id} watermark is misaligned");
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(series.Trainer.TimeZoneId);

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var todayLocal = DateOnly.FromDateTime(nowLocal);

            var targetDate = todayLocal.AddDays(90);

            if (interval <= 0) throw new InvalidOperationException("interval set to incorrect value");

            var next = series.MaterializedUntil.AddDays(interval);

            while(next <= targetDate)
            {
                if (next < todayLocal) continue;

                var localStart = next.ToDateTime(series.StartTime);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
                var endUtc = startUtc.AddMinutes(series.Duration);

                var newBookedSessionSlot = new MaterializedBookedSessionSlotDto
                {
                    TrainerId = series.TrainerId,
                    BookingSeries = series,
                    ClientId = series.ClientId,
                    Title = series.Title,
                    Description = series.Description,
                    StartDateTime = startUtc,
                    EndDateTime = endUtc
                };

                await unitOfWork.BookedSessionSlotRepository.AddBookedMaterializedSlotsAsync(newBookedSessionSlot);

                series.MaterializedUntil = DateOnly.FromDateTime(localStart);

                next = next.AddDays(interval);
            }
        }
    }
}
