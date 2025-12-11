using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace pto.track.services.DbContextStrategies
{
    /// <summary>
    /// Abstraction for registering a DbContext provider strategy.
    /// Implementations encapsulate provider-specific registration logic
    /// so tests and hosting environments can pick the correct setup.
    /// </summary>
    public interface IDbContextStrategy
    {
        /// <summary>
        /// Configure DbContext-related services on the provided IServiceCollection.
        /// </summary>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// True when this strategy targets an in-memory provider (useful for decisions in startup logic).
        /// </summary>
        bool IsInMemory { get; }
    }
}
