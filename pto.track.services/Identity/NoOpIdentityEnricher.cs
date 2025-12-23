using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pto.track.services.Identity;

/// <summary>
/// Default no-op implementation that returns no additional attributes.
/// </summary>
public class NoOpIdentityEnricher : IIdentityEnricher
{
    public Task<IDictionary<string, string?>> EnrichAsync(string normalizedIdentity, CancellationToken cancellationToken = default)
    {
        // Placeholder for future enrichment; returns empty.
        IDictionary<string, string?> result = new Dictionary<string, string?>();
        return Task.FromResult(result);
    }
}
