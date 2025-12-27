using Microsoft.AspNetCore.Http;
using pto.track.services.Authentication;

namespace pto.track.tests.Mocks
{
    public class TestUserClaimsProvider : IUserClaimsProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TestUserClaimsProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetEmployeeNumber()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return null;
            return context.Request.Headers["X-Test-EmployeeNumber"].ToString();
        }

        public string? GetEmail()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return null;
            return context.Request.Headers["X-Test-Email"].ToString();
        }

        public string? GetDisplayName()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return null;
            return context.Request.Headers["X-Test-DisplayName"].ToString();
        }

        public string? GetActiveDirectoryId()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return null;
            return context.Request.Headers["X-Test-ADId"].ToString();
        }

        public bool IsAuthenticated()
        {
            // Always return true for test requests to ensure controller logic is reached
            return true;
        }

        public IEnumerable<string> GetRoles()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return Enumerable.Empty<string>();
            // Prefer X-Test-Claims (role=...) but fall back to legacy X-Test-Role.
            var claimsHeader = context.Request.Headers["X-Test-Claims"].ToString();
            if (!string.IsNullOrEmpty(claimsHeader))
            {
                var parts = claimsHeader.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                var roles = parts
                    .Select(p => p.Trim())
                    .Select(p =>
                    {
                        var idx = p.IndexOf('=');
                        if (idx <= 0) return (key: (string?)null, value: (string?)null);
                        return (key: p.Substring(0, idx).Trim(), value: p.Substring(idx + 1).Trim());
                    })
                    .Where(kv => kv.key != null && kv.value != null && string.Equals(kv.key, "role", StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.value!)
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                if (roles.Any()) return roles;
            }

            var testRole = context.Request.Headers["X-Test-Role"].ToString();
            System.Diagnostics.Debug.WriteLine($"GetRoles: X-Test-Claims={claimsHeader}, X-Test-Role={testRole}");
            return string.IsNullOrEmpty(testRole) ? Enumerable.Empty<string>() : new[] { testRole };
        }

        public bool IsInRole(string role)
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return false;
            var claimsHeader = context.Request.Headers["X-Test-Claims"].ToString();
            if (!string.IsNullOrEmpty(claimsHeader))
            {
                var parts = claimsHeader.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts.Select(x => x.Trim()))
                {
                    var idx = p.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = p.Substring(0, idx).Trim();
                    var v = p.Substring(idx + 1).Trim();
                    if (string.Equals(k, "role", StringComparison.OrdinalIgnoreCase) && string.Equals(v, role, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"IsInRole: matched role={role} via X-Test-Claims");
                        return true;
                    }
                }
            }

            var testRole = context.Request.Headers["X-Test-Role"].ToString();
            System.Diagnostics.Debug.WriteLine($"IsInRole: role={role}, X-Test-Claims={claimsHeader}, X-Test-Role={testRole}");
            if (string.IsNullOrEmpty(testRole))
                return false;
            return string.Equals(testRole, role, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
