using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class BookedSessionSlotRepository(DataContext context, IMapper mapper) : IBookedSessionSlotRepository
    {
        public async Task<BookedSessionSlot?> GetTrainerBookedSessionSlotByIdAsync(int trainerId, int sessionId)
        {
            return await context.BookedSessionsSlot.FirstOrDefaultAsync(s => s.Id == sessionId && s.TrainerId == trainerId);
        }
        public async Task<List<BookedSessionSlotViewDto>> GetTrainerBookedSessionSlotsInRangeAsync(int trainerId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            return await context.BookedSessionsSlot
                .Where(s => s.TrainerId == trainerId && s.StartDateTime >= rangeStartUtc && s.StartDateTime < rangeEndUtc)
                .OrderBy(s => s.StartDateTime)
                .Select(s => new BookedSessionSlotViewDto
                {
                    Id = s.Id,
                    ClientId = s.ClientId,
                    ClientName = s.Client.FirstName,
                    BookingSeriesId = s.BookingSeriesId,
                    Title = s.Title,
                    Description = s.Description,
                    StartDateTime = s.StartDateTime,
                    EndDateTime = s.EndDateTime,
                    Status = s.Status
                })
                .ToListAsync();
        }

        public void UpdateBookedSessionSlot(BookedSessionSlot bookedSessionSlot, BookedSessionSlotDto updatedBookedSessionSlot)
        {
            mapper.Map(updatedBookedSessionSlot, bookedSessionSlot);
        }

        public async Task AddBookingSlotForTrainerAsync(Trainer trainer, BookedSessionSlotDto bookedSessionSlotDto)
        {
            var bookedSessionSlot = mapper.Map<BookedSessionSlot>(bookedSessionSlotDto);
            bookedSessionSlot.TrainerId = trainer.Id;

            await context.BookedSessionsSlot.AddAsync(bookedSessionSlot);
        }

        public async Task AddBookedMaterializedSlotsAsync(MaterializedBookedSessionSlotDto sessionSlotDto)
        {
            var bookedSessionSlot = mapper.Map<BookedSessionSlot>(sessionSlotDto);
            await context.BookedSessionsSlot.AddAsync(bookedSessionSlot);
        }

        public async Task RemoveBookedSessionSlotsAtCutOffDateAsync(int seriesId, DateTime dateCutOff)
        {
            var slotsToRemove = await context.BookedSessionsSlot
                .Where(b => b.BookingSeriesId == seriesId &&
                b.StartDateTime >= dateCutOff &&
                b.Status == SessionStatus.Scheduled) 
                .ToListAsync();

            context.BookedSessionsSlot.RemoveRange(slotsToRemove);
        }

        public async Task UpdateFutureScheduledSlotsToTimeZoneAsync(int trainerId, TimeZoneInfo oldZone, TimeZoneInfo newZone, DateTime fromUtc)
        {
            var slotsToShift = await context.BookedSessionsSlot
                .Where(s => s.TrainerId == trainerId &&
                s.StartDateTime >= fromUtc &&
                s.Status == SessionStatus.Scheduled)
                .ToListAsync();

            foreach (var slot in slotsToShift)
            {
                var duration = slot.EndDateTime - slot.StartDateTime;

                // keep the trainer's clock time: read it in the old zone, then place it in the new zone
                var localStart = TimeZoneInfo.ConvertTimeFromUtc(slot.StartDateTime, oldZone);
                localStart = DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified);

                if (newZone.IsInvalidTime(localStart)) // falls in the new zone's spring-forward gap
                {
                    localStart = localStart.AddHours(1);
                }

                slot.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(localStart, newZone);
                slot.EndDateTime = slot.StartDateTime.Add(duration);
            }
        }

        public async Task RemoveAllBookedSessionSlotsForUserAsync(UserBase user)
        {
            var slotsToRemove = user.Role == Enums.UserRole.Trainer ?
                await context.BookedSessionsSlot.Where(s => s.TrainerId == user.Id).ToListAsync() :
                await context.BookedSessionsSlot.Where(s => s.ClientId == user.Id).ToListAsync();

            context.BookedSessionsSlot.RemoveRange(slotsToRemove);
        }

        public void RemoveBookedSessionSlot(BookedSessionSlot sessionSlot)
        {
            context.Remove(sessionSlot);
        }

    }
}
