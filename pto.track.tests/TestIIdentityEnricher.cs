using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using pto.track.services.Identity;

namespace pto.track.tests
{
    // Test implementation of IIdentityEnricher that reads X-Test-Claims header
    // and returns a dictionary of attributes. Header format: comma-separated k=v pairs.
    public class TestIIdentityEnricher : IIdentityEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TestIIdentityEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Task<IDictionary<string, string?>> EnrichAsync(string normalizedIdentity, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return Task.FromResult<IDictionary<string, string?>>(dict);

            var header = ctx.Request.Headers["X-Test-Claims"].ToString();
            if (string.IsNullOrWhiteSpace(header)) return Task.FromResult<IDictionary<string, string?>>(dict);

            var parts = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts.Select(p => p.Trim()))
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;
                var k = part.Substring(0, idx).Trim();
                var v = part.Substring(idx + 1).Trim();
                dict[k] = v;
            }

            return Task.FromResult<IDictionary<string, string?>>(dict);
        }
    }
}
