using AutoMapper;
using ClientDashboard_API.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientDashboard_API_Tests
{
    /// <summary>
    /// Builds an <see cref="IMapper"/> from the production <see cref="AutoMapperProfiles"/> so tests
    /// exercise the real mapping configuration rather than a hand-maintained duplicate. Add new shared
    /// maps to that profile and every test picks them up automatically — no per-test-file changes.
    /// Pass <paramref name="extraMaps"/> only for test-only maps that don't belong in production
    /// (e.g. the entity identity maps used by the ML fixtures).
    /// </summary>
    public static class TestMapperFactory
    {
        public static IMapper Create(Action<IMapperConfigurationExpression>? extraMaps = null)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfiles());
                extraMaps?.Invoke(cfg);
            }, NullLoggerFactory.Instance);

            return config.CreateMapper();
        }
    }
}
