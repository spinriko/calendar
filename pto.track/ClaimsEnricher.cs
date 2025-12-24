using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace pto.track
{
    // Default, no-op claims enricher used in production. Test projects may provide
    // their own `IClaimsTransformation` implementation to inject test claims.
    public class ClaimsEnricher : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Intentionally no-op — return the principal unchanged.
            return Task.FromResult(principal);
        }
    }
}
