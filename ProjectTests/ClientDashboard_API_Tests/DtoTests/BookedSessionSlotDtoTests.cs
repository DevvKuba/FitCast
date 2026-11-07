using ClientDashboard_API.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API_Tests.DtoTests
{
    // Runs the same validation [ApiController] runs before an action, which direct controller calls in tests skip
    public class BookedSessionSlotDtoTests
    {
        private static readonly DateTime StartUtc = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Validate_PassesForUtcTimesWithEndAfterStart()
        {
            var results = ValidationErrors(SlotDto(StartUtc, StartUtc.AddHours(1)));

            Assert.Empty(results);
        }

        [Fact]
        public void Validate_FailsWhenEndIsBeforeStart()
        {
            var results = ValidationErrors(SlotDto(StartUtc, StartUtc.AddHours(-1)));

            var error = Assert.Single(results);
            Assert.Equal("Session end time must be after the start time", error.ErrorMessage);
        }

        [Fact]
        public void Validate_FailsWhenEndEqualsStart()
        {
            var results = ValidationErrors(SlotDto(StartUtc, StartUtc));

            var error = Assert.Single(results);
            Assert.Equal("Session end time must be after the start time", error.ErrorMessage);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Local)]
        public void Validate_FailsWhenTimesAreNotUtc(DateTimeKind kind)
        {
            var start = DateTime.SpecifyKind(StartUtc, kind);

            var results = ValidationErrors(SlotDto(start, start.AddHours(1)));

            var error = Assert.Single(results);
            Assert.Equal("Session times must be provided in UTC", error.ErrorMessage);
        }

        [Fact]
        public void Validate_FailsWhenOnlyOneTimeIsNotUtc()
        {
            var end = DateTime.SpecifyKind(StartUtc.AddHours(1), DateTimeKind.Unspecified);

            var results = ValidationErrors(SlotDto(StartUtc, end));

            var error = Assert.Single(results);
            Assert.Equal("Session times must be provided in UTC", error.ErrorMessage);
        }

        [Fact]
        public void Validate_ReportsBothErrorsWhenNotUtcAndEndBeforeStart()
        {
            var start = DateTime.SpecifyKind(StartUtc, DateTimeKind.Unspecified);

            var results = ValidationErrors(SlotDto(start, start.AddHours(-1)));

            Assert.Equal(2, results.Count);
        }

        private static List<ValidationResult> ValidationErrors(BookedSessionSlotDto dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
            return results;
        }

        private static BookedSessionSlotDto SlotDto(DateTime start, DateTime end) => new()
        {
            ClientId = 1,
            Title = "session",
            StartDateTime = start,
            EndDateTime = end
        };
    }
}
