using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class SupportedTimeZonesTests
    {
        public static TheoryData<string> AllZones()
        {
            var data = new TheoryData<string>();
            foreach (var id in SupportedTimeZones.All) data.Add(id);
            return data;
        }

        [Theory]
        [MemberData(nameof(AllZones))]
        public void EverySupportedZone_ResolvesOnThisPlatform(string timeZoneId)
        {
            // Catches typos and hosts where .NET can't resolve IANA IDs, before the
            // materialization job ever tries to convert a slot with one.
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            Assert.NotNull(zone);
        }

        [Fact]
        public void All_HasNoDuplicates()
        {
            Assert.Equal(SupportedTimeZones.All.Count, SupportedTimeZones.All.Distinct().Count());
        }

        [Fact]
        public void Default_IsInTheSupportedList()
        {
            Assert.True(SupportedTimeZones.IsSupported(SupportedTimeZones.Default));
        }

        [Theory]
        [InlineData("Europe/London", true)]
        [InlineData("America/New_York", true)]
        [InlineData("Mars/Olympus_Mons", false)]
        [InlineData("GMT Standard Time", false)]
        [InlineData("BST", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSupported_OnlyAcceptsListedIanaIds(string? timeZoneId, bool expected)
        {
            Assert.Equal(expected, SupportedTimeZones.IsSupported(timeZoneId));
        }

        [Fact]
        public void London_AppliesDaylightSavingPerDate()
        {
            // Same 18:00 local session either side of the October clock change lands on
            // different UTC hours - the behaviour materialization relies on.
            var london = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.London);

            var beforeChange = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 10, 20, 18, 0, 0), london);
            var afterChange = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 10, 27, 18, 0, 0), london);

            Assert.Equal(17, beforeChange.Hour);
            Assert.Equal(18, afterChange.Hour);
        }
    }
}
