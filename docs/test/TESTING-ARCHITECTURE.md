# Testing Architecture & Strategy

This document describes the PTO Track testing approach, including unit tests, integration tests, in-memory databases, authentication mocking, and CI/CD integration.

## Test Organization

PTO Track uses a **layered testing approach** with clear separation by scope:

```
pto.track.services.tests       → Service layer unit tests (business logic)
pto.track.data.tests            → Data access layer tests (EF Core, DB queries)
pto.track.tests                 → Integration tests (full TestHost, HTTP layer)
pto.track.tests.js              → Frontend tests (Jest, TypeScript/JavaScript)
```

## 1. Service Layer Tests (pto.track.services.tests)

**Scope**: Pure business logic, no database or HTTP

**Dependencies**: Mocked (interfaces, in-memory stubs)

### Example Test

```csharp
public class AbsenceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AbsenceService _service;

    public AbsenceServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new AbsenceService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task ApproveAbsence_ShouldUpdateStatusAndSaveChanges()
    {
        // Arrange
        var absence = new Absence { Id = 1, Status = AbsenceStatus.Pending };
        _mockUnitOfWork.Setup(u => u.AbsenceRepository.GetByIdAsync(1))
            .ReturnsAsync(absence);

        // Act
        await _service.ApproveAsync(1);

        // Assert
        Assert.Equal(AbsenceStatus.Approved, absence.Status);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
```

**Testing Framework**: xUnit + Moq

**Key Characteristics**:
- ✅ Fast (no I/O)
- ✅ Isolated (can run in parallel)
- ✅ Deterministic (no external dependencies)
- ✅ Easy to debug (pure logic)

## 2. Data Access Layer Tests (pto.track.data.tests)

**Scope**: EF Core queries, migrations, database schema validation

**Database**: In-memory SQLite or in-memory provider

### In-Memory Database Strategy

To avoid external database during test:

```csharp
public class AbsenceRepositoryTests
{
    private readonly PtoTrackDbContext _context;

    public AbsenceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PtoTrackDbContext>()
            .UseInMemoryDatabase("test_db_" + Guid.NewGuid())
            .Options;

        _context = new PtoTrackDbContext(options);
        _context.Database.EnsureCreated();  // Creates schema
    }

    [Fact]
    public async Task GetAbsencesByResourceAndPeriod_ShouldReturnMatchingRecords()
    {
        // Arrange
        var absences = new List<Absence>
        {
            new Absence { Id = 1, ResourceId = 1, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 1, 5) }
        };
        _context.Absences.AddRange(absences);
        _context.SaveChanges();

        var repository = new AbsenceRepository(_context);

        // Act
        var result = await repository.GetByResourceAndPeriodAsync(1, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Assert
        Assert.Single(result);
    }
}
```

**Pros**:
- ✅ No SQL Server required
- ✅ Isolated per test (new database per test)
- ✅ Fast (all in-memory)
- ✅ Can test LINQ queries

**Cons**:
- ⚠️ In-memory provider doesn't support all SQL Server features (e.g., `GROUP BY` edge cases)
- ⚠️ May hide real SQL issues (use SQLite for closer match to prod)

### Shared InMemoryDatabaseRoot

For tests that need **shared state** (e.g., seed data used across multiple tests):

```csharp
private static readonly InMemoryDatabaseRoot _inMemoryDatabaseRoot = new();

public DbContextOptions<PtoTrackDbContext> GetDbContextOptions()
{
    return new DbContextOptionsBuilder<PtoTrackDbContext>()
        .UseInMemoryDatabase("pto_track_shared", _inMemoryDatabaseRoot)
        .Options;
}
```

**Use case**: When multiple test methods need same data

## 3. Integration Tests (pto.track.tests)

**Scope**: Full application stack (HTTP, authentication, database, business logic)

**Test Host**: `WebApplicationFactory` (in-memory Kestrel + dependency injection)

**Database**: In-memory (EF Core)

**Authentication**: Mocked via custom identity enricher

### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PtoTrackDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Register in-memory DbContext
            services.AddDbContext<PtoTrackDbContext>(options =>
            {
                options.UseInMemoryDatabase("integration_test", _dbRoot);
            });

            // Seed test data
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
            context.Database.EnsureCreated();
            SeedTestData(context);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Override configuration for tests
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Authentication:Mode", "Mock" }
            });
        });
    }

    private void SeedTestData(PtoTrackDbContext context)
    {
        context.Resources.Add(new Resource { Id = 1, Name = "Alice" });
        context.SaveChanges();
    }
}
```

**Key Points**:
- `WebApplicationFactory` creates a test host with full DI container
- Replaces SQL Server DbContext with in-memory (isolated per test class)
- Registers mock authentication handler
- Runs database migrations automatically

### Example Integration Test

```csharp
public class AbsencesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AbsencesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAbsences_ShouldReturnOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/absences");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var absences = JsonSerializer.Deserialize<List<AbsenceDto>>(json);
        Assert.NotNull(absences);
    }

    [Fact]
    public async Task CreateAbsence_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var request = new CreateAbsenceRequest
        {
            ResourceId = 1,
            StartDate = new DateTime(2025, 2, 1),
            EndDate = new DateTime(2025, 2, 5),
            Type = "PTO"
        };
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync("/api/absences", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## 4. Authentication Mocking

PTO Track uses **Windows Authentication** in production, but tests need a simple mock.

### Test Identity Enricher

```csharp
public class TestIdentityEnricher : IClaimsTransformation
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestIdentityEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request?.Headers.TryGetValue("X-Test-Claims", out var claimsHeader) ?? false)
        {
            var claims = ParseTestClaims(claimsHeader.ToString());
            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }

        return principal;
    }

    private List<Claim> ParseTestClaims(string header)
    {
        // Format: "role:admin,name:alice@example.com"
        return header.Split(',')
            .Select(kvp => kvp.Trim().Split(':'))
            .Where(parts => parts.Length == 2)
            .Select(parts => new Claim(parts[0], parts[1]))
            .ToList();
    }
}
```

### Registering Mock Auth

In `Program.cs` or test factory:

```csharp
if (env.IsDevelopment() || env.IsEnvironment("Testing"))
{
    services.AddAuthentication("TestScheme")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", null);
    
    services.AddScoped<IClaimsTransformation, TestIdentityEnricher>();
}
```

### Using Mock Claims in Tests

```csharp
[Fact]
public async Task ApproveAbsence_WithApproverRole_ShouldSucceed()
{
    // Add test user with approver role
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/absences/1/approve")
    {
        Headers =
        {
            { "X-Test-Claims", "role:approver,name:bob@example.com" }
        }
    };

    var response = await _client.SendAsync(request);

    Assert.True(response.IsSuccessStatusCode);
}
```

## 5. Frontend Tests (pto.track.tests.js)

**Framework**: Jest

**Coverage**: Unit tests for components, utilities, and async flows

### Example Jest Test

```typescript
import { parseAbsenceType } from '../src/utils/absence';

describe('AbsenceUtils', () => {
  test('parseAbsenceType converts string to enum', () => {
    expect(parseAbsenceType('PTO')).toBe('PTO');
    expect(parseAbsenceType('SICK')).toBe('SICK');
  });

  test('validateAbsencePeriod rejects invalid dates', () => {
    const startDate = new Date('2025-02-05');
    const endDate = new Date('2025-02-01');

    expect(() => validateAbsencePeriod(startDate, endDate)).toThrow();
  });
});
```

**Running**:
```powershell
cd pto.track.tests.js
npm ci
npm test
```

## Test Execution in CI/CD

### Build Stage: Run Tests

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/*tests.csproj'
    arguments: '/p:RunAnalyzersDuringBuild=false'
    publishTestResults: true
    testRunTitle: '.NET Tests'
```

### Parallel Execution

Tests run in parallel by default (faster pipeline):

```powershell
dotnet test --parallel:max
```

**Caveat**: `InMemoryDatabaseRoot` must be isolated per test class (avoid shared state).

### Coverage Reporting

```powershell
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Merge coverage from all projects
reportgenerator -reports:"**/*coverage.xml" -targetdir:"coverage-report"
```

## Test Data Strategy

### Option 1: Seed in Factory

```csharp
private void SeedTestData(PtoTrackDbContext context)
{
    context.Resources.Add(new Resource { Id = 1, Name = "Alice" });
    context.Absences.Add(new Absence { Id = 1, ResourceId = 1, ... });
    context.SaveChanges();
}
```

**Pros**: Simple, isolated
**Cons**: Data duplication across tests

### Option 2: Builder Pattern

```csharp
public class AbsenceBuilder
{
    private int _id = 1;
    private int _resourceId = 1;
    private DateTime _startDate = new DateTime(2025, 2, 1);
    private AbsenceStatus _status = AbsenceStatus.Pending;

    public AbsenceBuilder WithStatus(AbsenceStatus status)
    {
        _status = status;
        return this;
    }

    public Absence Build()
    {
        return new Absence
        {
            Id = _id,
            ResourceId = _resourceId,
            StartDate = _startDate,
            Status = _status
        };
    }
}

// Usage
var absence = new AbsenceBuilder()
    .WithStatus(AbsenceStatus.Approved)
    .Build();
```

**Pros**: Flexible, readable, reduces boilerplate
**Cons**: More code upfront

### Option 3: Test Fixtures

```csharp
public class AbsenceTestFixture : IDisposable
{
    public PtoTrackDbContext Context { get; private set; }
    public List<Absence> SampleAbsences { get; private set; }

    public AbsenceTestFixture()
    {
        Context = new PtoTrackDbContext(...);
        SampleAbsences = CreateSampleAbsences();
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}

// Usage
public class AbsenceTests : IClassFixture<AbsenceTestFixture>
{
    public AbsenceTests(AbsenceTestFixture fixture)
    {
        // Use fixture.SampleAbsences
    }
}
```

**Pros**: Reusable, setup/teardown handled
**Cons**: Hidden dependencies, harder to trace

## Common Testing Patterns

### Testing Authorization

```csharp
[Fact]
public async Task DeleteAbsence_WithoutApproverRole_ShouldReturn403()
{
    var request = new HttpRequestMessage(HttpMethod.Delete, "/api/absences/1")
    {
        Headers =
        {
            { "X-Test-Claims", "role:employee" }  // Not approver
        }
    };

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

### Testing Validation

```csharp
[Theory]
[InlineData(null)]  // Null name
[InlineData("")]    // Empty name
public async Task CreateResource_WithInvalidName_ShouldReturn400(string name)
{
    var request = new CreateResourceRequest { Name = name };
    var response = await _client.PostAsync("/api/resources", JsonContent.Create(request));

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

### Testing Edge Cases

```csharp
[Fact]
public async Task GetAbsences_WithEmptyDatabase_ShouldReturnEmptyList()
{
    var response = await _client.GetAsync("/api/absences");
    var absences = await response.Content.ReadAsAsync<List<AbsenceDto>>();

    Assert.Empty(absences);
}

[Fact]
public async Task ApproveAbsence_WhenAlreadyApproved_ShouldReturnConflict()
{
    // Create absence and approve it
    var absence = new Absence { Status = AbsenceStatus.Approved };
    // ...

    // Try to approve again
    var response = await _client.PostAsync($"/api/absences/{absence.Id}/approve", ...);

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

## Running Tests Locally

### All Tests

```powershell
dotnet test pto.track.sln
```

### Single Project

```powershell
dotnet test pto.track.services.tests/pto.track.services.tests.csproj
```

### Single Test Class

```powershell
dotnet test --filter "FullyQualifiedName~AbsenceServiceTests"
```

### Single Test Method

```powershell
dotnet test --filter "FullyQualifiedName~AbsenceServiceTests.ApproveAbsence_ShouldUpdateStatusAndSaveChanges"
```

### Without Analyzer (Faster)

```powershell
dotnet test /p:RunAnalyzersDuringBuild=false
```

### With Coverage

```powershell
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Debugging Tests

### In Visual Studio

1. Set breakpoint in test method
2. **Test Explorer** (View → Test Explorer)
3. Right-click test → **Debug**

### In VS Code

1. Set breakpoint
2. **Run and Debug** (Ctrl+Shift+D)
3. Select `.NET Core` launch config
4. Press `F5`

### PowerShell Debugging

```powershell
dotnet test --verbose --logger "console;verbosity=detailed"
```

## Test Coverage Goals

| Layer | Target Coverage | Justification |
|-------|-----------------|---------------|
| **Services** | 80%+ | Core business logic |
| **Data** | 70%+ | Repository patterns |
| **Controllers** | 60%+ | Authorization, edge cases |
| **Frontend** | 50%+ | Critical paths, utilities |

## CI/CD Integration

### Test Results in Pipeline

Azure Pipelines publishes test results:

```
Test Results Tab
├─ Failed (red)
├─ Passed (green)
└─ Skipped (gray)

Code Coverage Tab
├─ Overall % coverage
├─ Per-project breakdown
└─ Trend chart
```

### Blocking on Test Failures

```yaml
- task: PublishTestResults@2
  condition: succeeded()  # Only if tests passed
  inputs:
    testResultsFormat: VSTest
```

### Manual Test Run (Debugging Pipeline Tests)

If tests fail in CI but pass locally:

```powershell
# Simulate CI environment
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:SKIP_CODE_METRICS = "1"

dotnet test pto.track.sln --verbose
```

---

**See also**:
- [Pipeline Overview](../ci/PIPELINE-OVERVIEW.md) — How tests are run in CI
- [Developer Setup](../run/DEVELOPER-SETUP.md) — Local test execution
- [RUN-LOCAL.md](../run/RUN-LOCAL.md) — Detailed testing guide
