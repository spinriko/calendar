using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace pto.track.tests
{
    // Test-only IClaimsTransformation that reads the X-Test-Claims header and
    // augments the current ClaimsPrincipal. Header format is comma-separated
    // key=value pairs, e.g. "role=Admin,role=Approver,name=Test User,email=test@example.com".
    public class TestIdentityEnricher : IClaimsTransformation
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TestIdentityEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null || principal == null)
            {
                return Task.FromResult(principal);
            }

            var header = context.Request.Headers["X-Test-Claims"].ToString();
            if (string.IsNullOrWhiteSpace(header))
            {
                return Task.FromResult(principal);
            }

            var claims = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Select(part =>
                {
                    var idx = part.IndexOf('=');
                    if (idx <= 0) return (type: (string)null, value: (string)null);
                    var k = part.Substring(0, idx).Trim();
                    var v = part.Substring(idx + 1).Trim();
                    return (type: k, value: v);
                })
                .Where(kv => kv.type != null && kv.value != null)
                .ToList();

            if (!claims.Any()) return Task.FromResult(principal);

            // Preserve existing claims and authentication type, then append new claims
            var existingClaims = principal.Claims ?? Enumerable.Empty<Claim>();
            var authType = principal.Identity?.AuthenticationType;
            var identity = new ClaimsIdentity(existingClaims, authType, ClaimTypes.Name, ClaimTypes.Role);

            foreach (var (type, value) in claims)
            {
                if (string.Equals(type, "role", StringComparison.OrdinalIgnoreCase))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, value ?? string.Empty));
                }
                else if (string.Equals(type, "name", StringComparison.OrdinalIgnoreCase))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, value ?? string.Empty));
                }
                else
                {
                    identity.AddClaim(new Claim(type ?? string.Empty, value ?? string.Empty));
                }
            }

            var newPrincipal = new ClaimsPrincipal(identity);
            return Task.FromResult(newPrincipal);
        }
    }
}
