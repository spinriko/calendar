using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pto.track.services.Identity;

/// <summary>
/// Enriches an identity with additional attributes from external sources (e.g., AD, internal services).
/// </summary>
public interface IIdentityEnricher
{
    /// <summary>
    /// Enriches the provided normalized identity name with additional attributes.
    /// Implementations may call external systems; they should be resilient and fast.
    /// </summary>
    /// <param name="normalizedIdentity">Normalized identity key (e.g., samAccountName).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Additional attributes keyed by name.</returns>
    Task<IDictionary<string, string?>> EnrichAsync(string normalizedIdentity, CancellationToken cancellationToken = default);
}
