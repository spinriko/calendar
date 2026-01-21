using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using pto.track;
using pto.track.services.Identity;
using Xunit;

namespace pto.track.tests
{
    public class ClaimsEnricherTests
    {
        [Fact]
        public async Task TransformAsync_UnauthenticatedUser_ReturnsUnchangedPrincipal()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var adService = Mock.Of<IActiveDirectoryService>();
            var enricher = new ClaimsEnricher(logger, adService);
            
            var identity = new ClaimsIdentity(); // Not authenticated
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.Same(principal, result);
            Assert.False(result.Identity?.IsAuthenticated);
        }

        [Fact]
        public async Task TransformAsync_AlreadyEnriched_ReturnsUnchangedPrincipal()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var adService = Mock.Of<IActiveDirectoryService>();
            var enricher = new ClaimsEnricher(logger, adService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\testuser"));
            identity.AddClaim(new Claim("ad_enriched", "true"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.Same(principal, result);
            // Verify AD service was never called
            Mock.Get(adService).Verify(s => s.GetUserAttributesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task TransformAsync_EmployeeUser_EnrichesWithEmployeeAttributes()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            var enricher = new ClaimsEnricher(logger, mockAdService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\testuser"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.True(result.Identity?.IsAuthenticated);
            Assert.Contains(result.Claims, c => c.Type == "employeeID" && c.Value == "12345");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Upn && c.Value == "testuser@example.com");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Email && c.Value == "testuser@example.com");
            Assert.Contains(result.Claims, c => c.Type == "displayName" && c.Value == "Test User");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.GivenName && c.Value == "Test User");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value.Contains("Employees"));
            Assert.Contains(result.Claims, c => c.Type == "ad_enriched" && c.Value == "true");
        }

        [Fact]
        public async Task TransformAsync_ManagerUser_EnrichesWithManagerAttributes()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            var enricher = new ClaimsEnricher(logger, mockAdService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\manager"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.True(result.Identity?.IsAuthenticated);
            Assert.Contains(result.Claims, c => c.Type == "employeeID" && c.Value == "67890");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Upn && c.Value == "manager@example.com");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Email && c.Value == "manager@example.com");
            Assert.Contains(result.Claims, c => c.Type == "displayName" && c.Value == "Test Manager");
            
            // Manager should have multiple role claims
            var roleClaims = result.FindAll(ClaimTypes.Role).ToList();
            Assert.Contains(roleClaims, c => c.Value.Contains("Managers"));
            Assert.Contains(roleClaims, c => c.Value.Contains("Approvers"));
            Assert.Equal(2, roleClaims.Count);
        }

        [Fact]
        public async Task TransformAsync_AdminUser_EnrichesWithAdminAttributes()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            var enricher = new ClaimsEnricher(logger, mockAdService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\admin"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.True(result.Identity?.IsAuthenticated);
            Assert.Contains(result.Claims, c => c.Type == "employeeID" && c.Value == "99999");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Upn && c.Value == "admin@example.com");
            Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Email && c.Value == "admin@example.com");
            Assert.Contains(result.Claims, c => c.Type == "displayName" && c.Value == "Test Administrator");
            
            // Admin should have multiple role claims
            var roleClaims = result.FindAll(ClaimTypes.Role).ToList();
            Assert.Contains(roleClaims, c => c.Value.Contains("Domain Admins"));
            Assert.Contains(roleClaims, c => c.Value.Contains("Administrators"));
            Assert.Equal(2, roleClaims.Count);
        }

        [Fact]
        public async Task TransformAsync_UserNotFoundInAD_EnrichesWithoutAttributes()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            var enricher = new ClaimsEnricher(logger, mockAdService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\unknownuser"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.True(result.Identity?.IsAuthenticated);
            // Should not have any AD attributes
            Assert.DoesNotContain(result.Claims, c => c.Type == "employeeID");
            Assert.DoesNotContain(result.Claims, c => c.Type == "ad_enriched");
        }

        [Fact]
        public async Task TransformAsync_UsernameWithoutDomain_ExtractsCorrectly()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            var enricher = new ClaimsEnricher(logger, mockAdService);
            
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "testuser")); // No domain prefix
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.Contains(result.Claims, c => c.Type == "employeeID" && c.Value == "12345");
        }

        [Fact]
        public async Task TransformAsync_CustomUser_AddsCustomAttributes()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = new MockActiveDirectoryService(Mock.Of<ILogger<MockActiveDirectoryService>>());
            
            // Add a custom test user
            mockAdService.AddTestUser("customuser", new AdUserAttributes
            {
                EmployeeId = "55555",
                UserPrincipalName = "custom@example.com",
                Mail = "custom@example.com",
                DisplayName = "Custom Test User",
                MemberOf = new List<string>
                {
                    "CN=CustomGroup1,OU=Groups,DC=example,DC=com",
                    "CN=CustomGroup2,OU=Groups,DC=example,DC=com"
                }
            });
            
            var enricher = new ClaimsEnricher(logger, mockAdService);
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\customuser"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert
            Assert.Contains(result.Claims, c => c.Type == "employeeID" && c.Value == "55555");
            Assert.Contains(result.Claims, c => c.Type == "displayName" && c.Value == "Custom Test User");
            
            var roleClaims = result.FindAll(ClaimTypes.Role).ToList();
            Assert.Contains(roleClaims, c => c.Value.Contains("CustomGroup1"));
            Assert.Contains(roleClaims, c => c.Value.Contains("CustomGroup2"));
            Assert.Equal(2, roleClaims.Count);
        }

        [Fact]
        public async Task TransformAsync_ADServiceThrows_DoesNotFailAuthentication()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ClaimsEnricher>>();
            var mockAdService = Mock.Of<IActiveDirectoryService>();
            Mock.Get(mockAdService)
                .Setup(s => s.GetUserAttributesAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("AD connection failed"));
            
            var enricher = new ClaimsEnricher(logger, mockAdService);
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "DOMAIN\\testuser"));
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = await enricher.TransformAsync(principal);

            // Assert - Should not throw, authentication continues without enrichment
            Assert.True(result.Identity?.IsAuthenticated);
            Assert.DoesNotContain(result.Claims, c => c.Type == "employeeID");
        }
    }
}
