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
            var testRole = context.Request.Headers["X-Test-Role"].ToString();
            System.Diagnostics.Debug.WriteLine($"GetRoles: X-Test-Role={testRole}");
            return string.IsNullOrEmpty(testRole) ? Enumerable.Empty<string>() : new[] { testRole };
        }

        public bool IsInRole(string role)
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return false;
            var testRole = context.Request.Headers["X-Test-Role"].ToString();
            System.Diagnostics.Debug.WriteLine($"IsInRole: role={role}, X-Test-Role={testRole}");
            if (string.IsNullOrEmpty(testRole))
                return false;
            return string.Equals(testRole, role, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
