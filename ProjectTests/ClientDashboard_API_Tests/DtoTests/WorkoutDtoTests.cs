using ClientDashboard_API.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API_Tests.DtoTests
{
    // Runs the same validation [ApiController] runs before an action, which direct controller calls in tests skip
    public class WorkoutDtoTests
    {
        private static readonly DateTime StartUtc = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void WorkoutAddDto_PassesWithADurationAndUtcStart()
        {
            Assert.Empty(ValidationErrors(AddDto(60, StartUtc)));
        }

        [Fact]
        public void WorkoutAddDto_PassesWithoutAStart()
        {
            Assert.Empty(ValidationErrors(AddDto(60, null)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-15)]
        public void WorkoutAddDto_FailsWithoutAPositiveDuration(int duration)
        {
            var error = Assert.Single(ValidationErrors(AddDto(duration, StartUtc)));
            Assert.Equal("A session duration must be provided", error.ErrorMessage);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Local)]
        public void WorkoutAddDto_FailsWhenTheStartIsNotUtc(DateTimeKind kind)
        {
            var error = Assert.Single(ValidationErrors(AddDto(60, DateTime.SpecifyKind(StartUtc, kind))));
            Assert.Equal("Session start time must be provided in UTC", error.ErrorMessage);
        }

        [Fact]
        public void WorkoutUpdateDto_PassesWithADurationAndUtcStart()
        {
            Assert.Empty(ValidationErrors(UpdateDto(60, StartUtc)));
        }

        [Fact]
        public void WorkoutUpdateDto_FailsWithoutAPositiveDuration()
        {
            var error = Assert.Single(ValidationErrors(UpdateDto(0, null)));
            Assert.Equal("A session duration must be provided", error.ErrorMessage);
        }

        [Fact]
        public void WorkoutUpdateDto_FailsWhenTheStartIsNotUtc()
        {
            var error = Assert.Single(ValidationErrors(UpdateDto(60, DateTime.SpecifyKind(StartUtc, DateTimeKind.Unspecified))));
            Assert.Equal("Session start time must be provided in UTC", error.ErrorMessage);
        }

        private static List<ValidationResult> ValidationErrors(object dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
            return results;
        }

        private static WorkoutAddDto AddDto(int duration, DateTime? startUtc) => new()
        {
            WorkoutTitle = "session",
            ClientName = "rob",
            ClientId = 1,
            SessionDate = "10/11/2026",
            Duration = duration,
            StartDateTime = startUtc
        };

        private static WorkoutUpdateDto UpdateDto(int duration, DateTime? startUtc) => new()
        {
            Id = 1,
            WorkoutTitle = "session",
            SessionDate = "10/11/2026",
            Duration = duration,
            StartDateTime = startUtc
        };
    }
}
