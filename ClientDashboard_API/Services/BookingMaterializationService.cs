using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;

namespace ClientDashboard_API.Services
{
    public class BookingMaterializationService(IUnitOfWork unitOfWork) : IBookingMaterializationService
    {
        public Task MaterializeBookingSeriesAsync(BookingSeries series)
        {
            // Watermark rule: MaterializedUntil = date of the LAST slot already generated for this series.
            // On series creation (in the controller), set it to StartDate.AddDays(-(int)Recurrence)
            // so the first step of the loop below lands exactly on StartDate.

            // 1. Work in the trainer's local time zone.
            //    tz         = constant "Europe/London" for now (per-trainer TimeZoneId later)
            //    todayLocal = local date of DateTime.UtcNow in tz
            //    targetDate = todayLocal + 90 days (keep 90 as a constant on this service, not in callers)

            // 2. interval = (int)series.Recurrence (7 or 14)
            //    Guard: if interval <= 0, return/throw - a 0 interval would loop forever.

            // 3. next = series.MaterializedUntil.AddDays(interval)
            //    while (next <= targetDate)
            //    {
            //        a. If next < todayLocal (back-dated series): don't create a slot, just advance.
            //        b. Build the times in local, then convert:
            //           localStart = next.ToDateTime(series.StartTime)
            //           startUtc   = TimeZoneInfo.ConvertTimeToUtc(localStart, tz)
            //           endUtc     = startUtc.AddMinutes(series.Duration)
            //        c. Stage a new BookedSessionSlot via the slot repository:
            //           TrainerId, ClientId, Title, Description copied from series,
            //           BookingSeries = series (navigation, NOT BookingSeriesId - a new series
            //           has Id 0 until it's saved), StartDateTime = startUtc,
            //           EndDateTime = endUtc, Status = SessionStatus.Scheduled
            //        d. series.MaterializedUntil = next;
            //           next = next.AddDays(interval);
            //    }

            // 4. No Complete() here - the caller (controller on create, job nightly) saves once.
        }
    }
}
