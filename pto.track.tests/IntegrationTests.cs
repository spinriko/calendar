using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pto.track.data;
using pto.track.services.DTOs;
using Xunit;

namespace pto.track.tests.Integration
{
    public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public IntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private CancellationToken GetTimeoutToken()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return cts.Token;
        }

        private HttpClient GetClientWithInMemoryDb(Action<PtoTrackDbContext>? seed = null)
        {
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                // Ensure the app does not register the real SQL Server provider during tests
                builder.UseSetting("environment", "Testing");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:PtoTrackDbContext"] = string.Empty
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registrations (both the context type and its options)
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<PtoTrackDbContext>) ||
                        d.ServiceType == typeof(PtoTrackDbContext) ||
                        (d.ImplementationType != null && d.ImplementationType == typeof(PtoTrackDbContext))
                    ).ToList();

                    foreach (var d in descriptors)
                    {
                        services.Remove(d);
                    }

                    // Remove EF Core SQL Server provider service registrations which may have been added
                    var providerDescriptors = services.Where(d =>
                        (d.ServiceType?.Namespace != null && d.ServiceType.Namespace.Contains("SqlServer")) ||
                        (d.ImplementationType?.Namespace != null && d.ImplementationType.Namespace.Contains("SqlServer")) ||
                        (d.ServiceType?.FullName != null && d.ServiceType.FullName.Contains("SqlServer")) ||
                        (d.ImplementationType?.FullName != null && d.ImplementationType.FullName.Contains("SqlServer"))
                    ).ToList();

                    foreach (var d in providerDescriptors)
                    {
                        services.Remove(d);
                    }

                    var dbName = "IntegrationTestDb_" + Guid.NewGuid().ToString();
                    services.AddDbContext<PtoTrackDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
                        db.Database.EnsureCreated();
                        // After the test host has been created, seed the per-test in-memory DB
                        // so the running application observes the expected initial state.
                        if (seed != null)
                        {
                            var existingResources = db.Resources.ToList();
                            if (existingResources.Any()) db.Resources.RemoveRange(existingResources);

                            var existingEvents = db.Events.ToList();
                            if (existingEvents.Any()) db.Events.RemoveRange(existingEvents);

                            db.SaveChanges();
                            seed.Invoke(db);
                            try { db.SaveChanges(); } catch (ArgumentException) { }
                        }
                        else
                        {
                            // Use centralized seed helper for default data
                            pto.track.data.SeedDefaults.EnsureSeedData(db);
                        }
                    }
                });
            });

            var client = factory.CreateClient();

            // Also seed via the test host's service provider so the running
            // application instance observes the seeded data (the BuildServiceProvider
            // call above may create a separate provider). This ensures the controller
            // sees the same in-memory DB contents.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
                db.Database.EnsureCreated();

                if (seed != null)
                {
                    var existingResources = db.Resources.ToList();
                    if (existingResources.Any()) db.Resources.RemoveRange(existingResources);

                    var existingEvents = db.Events.ToList();
                    if (existingEvents.Any()) db.Events.RemoveRange(existingEvents);

                    db.SaveChanges();
                    seed.Invoke(db);
                    try { db.SaveChanges(); } catch (ArgumentException) { }
                }
                else
                {
                    pto.track.data.SeedDefaults.EnsureSeedData(db);
                }
            }

            return client;
        }

        [Fact]
        public async Task GetResources_ReturnsSeededResources()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                db.Resources.Add(new Resource { Id = 1, Name = "R1" });
                db.Resources.Add(new Resource { Id = 2, Name = "R2" });
            });

            var resp = await client.GetAsync("/api/resources", GetTimeoutToken());
            resp.EnsureSuccessStatusCode();
            var resources = await resp.Content.ReadFromJsonAsync<List<Resource>>();
            Assert.NotNull(resources);
            Assert.Equal(2, resources.Count);
        }

        [Fact]
        public async Task GetEvents_ReturnsSeededEventsForRange()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                db.Events.Add(new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "E1", ResourceId = 1 });
                db.Events.Add(new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 14, 10, 0, 0), End = new DateTime(2025, 11, 14, 11, 0, 0), Text = "E2", ResourceId = 1 });
            });

            var start = "2025-11-13T00:00:00";
            var end = "2025-11-13T23:59:59";
            var resp = await client.GetAsync($"/api/events?start={WebUtility.UrlEncode(start)}&end={WebUtility.UrlEncode(end)}", GetTimeoutToken());
            resp.EnsureSuccessStatusCode();
            var events = await resp.Content.ReadFromJsonAsync<List<SchedulerEvent>>();
            Assert.NotNull(events);
            Assert.Single(events);
        }

        [Fact]
        public async Task Events_EndToEnd_CRUD_Works()
        {
            var client = GetClientWithInMemoryDb();

            // Create
            var newEvent = new CreateEventDto(
                Start: new DateTime(2025, 11, 13, 10, 0, 0),
                End: new DateTime(2025, 11, 13, 11, 0, 0),
                Text: "E2E Event",
                Color: null,
                ResourceId: 1
            );

            var postResp = await client.PostAsJsonAsync("/api/events", newEvent, GetTimeoutToken());
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
            var created = await postResp.Content.ReadFromJsonAsync<EventDto>();
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);

            var id = created.Id;

            // Read
            var getResp = await client.GetAsync($"/api/events/{id}", GetTimeoutToken());
            getResp.EnsureSuccessStatusCode();
            var fetched = await getResp.Content.ReadFromJsonAsync<EventDto>();
            Assert.NotNull(fetched);
            Assert.Equal("E2E Event", fetched.Text);

            // Update
            var updateDto = new UpdateEventDto(
                Start: fetched.Start,
                End: fetched.End,
                Text: "E2E Updated",
                Color: fetched.Color,
                ResourceId: fetched.ResourceId
            );
            var putResp = await client.PutAsJsonAsync($"/api/events/{id}", updateDto, GetTimeoutToken());
            Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

            // Read back updated
            var getResp2 = await client.GetAsync($"/api/events/{id}", GetTimeoutToken());
            getResp2.EnsureSuccessStatusCode();
            var fetched2 = await getResp2.Content.ReadFromJsonAsync<EventDto>();
            Assert.NotNull(fetched2);
            Assert.Equal("E2E Updated", fetched2.Text);

            // Delete
            var delResp = await client.DeleteAsync($"/api/events/{id}", GetTimeoutToken());
            Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

            // Verify deleted
            var getAfterDel = await client.GetAsync($"/api/events/{id}", GetTimeoutToken());
            Assert.Equal(HttpStatusCode.NotFound, getAfterDel.StatusCode);
        }

        [Fact]
        public async Task PostSchedulerEvent_InvalidDates_ReturnsBadRequest()
        {
            var client = GetClientWithInMemoryDb();

            var badEvent = new SchedulerEvent
            {
                Start = new DateTime(2025, 11, 14, 11, 0, 0),
                End = new DateTime(2025, 11, 14, 10, 0, 0), // End before Start
                Text = "Invalid",
                ResourceId = 1
            };

            var resp = await client.PostAsJsonAsync("/api/events", badEvent, GetTimeoutToken());
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task PutSchedulerEvent_InvalidDates_ReturnsBadRequest()
        {
            var eventId = Guid.NewGuid();
            var client = GetClientWithInMemoryDb(db =>
            {
                db.Events.Add(new SchedulerEvent { Id = eventId, Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "t", ResourceId = 1 });
            });

            // Fetch the created event
            var get = await client.GetAsync($"/api/events/{eventId}", GetTimeoutToken());
            get.EnsureSuccessStatusCode();
            var ev = await get.Content.ReadFromJsonAsync<SchedulerEvent>();
            Assert.NotNull(ev);

            // Set invalid dates
            ev.Start = new DateTime(2025, 11, 15, 12, 0, 0);
            ev.End = new DateTime(2025, 11, 15, 11, 0, 0);

            var put = await client.PutAsJsonAsync($"/api/events/{ev.Id}", ev, GetTimeoutToken());
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }

        [Fact]
        public async Task GetResourcesByGroup_ReturnsGroupResources()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                // Remove existing groups if any
                var existingGroups = db.Groups.ToList();
                if (existingGroups.Any())
                {
                    db.Groups.RemoveRange(existingGroups);
                    db.SaveChanges();
                }

                // Create groups
                db.Groups.Add(new pto.track.data.Models.Group { GroupId = 1, Name = "Group 1" });
                db.Groups.Add(new pto.track.data.Models.Group { GroupId = 2, Name = "Group 2" });
                db.SaveChanges();

                // Create resources
                db.Resources.Add(new Resource { Id = 1, Name = "Group 1 Resource A", GroupId = 1 });
                db.Resources.Add(new Resource { Id = 2, Name = "Group 1 Resource B", GroupId = 1 });
                db.Resources.Add(new Resource { Id = 3, Name = "Group 2 Resource C", GroupId = 2 });
            });

            // Act
            var resp = await client.GetAsync("/api/resources/group/1", GetTimeoutToken());

            // Assert
            resp.EnsureSuccessStatusCode();
            var resources = await resp.Content.ReadFromJsonAsync<List<ResourceDto>>();
            Assert.NotNull(resources);
            Assert.Equal(2, resources.Count);
            Assert.All(resources, r => Assert.Equal(1, r.GroupId));
            Assert.Contains(resources, r => r.Id == 1);
            Assert.Contains(resources, r => r.Id == 2);
            Assert.DoesNotContain(resources, r => r.Id == 3);
        }

        [Fact]
        public async Task GetResourcesByGroup_WithValidGroup_Returns200()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                // Remove existing groups if any
                var existingGroups = db.Groups.ToList();
                if (existingGroups.Any())
                {
                    db.Groups.RemoveRange(existingGroups);
                    db.SaveChanges();
                }

                db.Groups.Add(new pto.track.data.Models.Group { GroupId = 1, Name = "Test Group" });
                db.SaveChanges();
                db.Resources.Add(new Resource { Id = 1, Name = "Test Resource", GroupId = 1 });
            });

            // Act
            var resp = await client.GetAsync("/api/resources/group/1", GetTimeoutToken());

            // Assert
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var resources = await resp.Content.ReadFromJsonAsync<List<ResourceDto>>();
            Assert.NotNull(resources);
            Assert.Single(resources);
        }

        [Fact]
        public async Task GetResourcesByGroup_WithInvalidGroup_ReturnsEmpty()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                // Remove existing groups if any
                var existingGroups = db.Groups.ToList();
                if (existingGroups.Any())
                {
                    db.Groups.RemoveRange(existingGroups);
                    db.SaveChanges();
                }

                db.Groups.Add(new pto.track.data.Models.Group { GroupId = 1, Name = "Test Group" });
                db.SaveChanges();
                db.Resources.Add(new Resource { Id = 1, Name = "Test Resource", GroupId = 1 });
            });

            // Act - Request resources for non-existent group
            var resp = await client.GetAsync("/api/resources/group/99", GetTimeoutToken());

            // Assert
            resp.EnsureSuccessStatusCode();
            var resources = await resp.Content.ReadFromJsonAsync<List<ResourceDto>>();
            Assert.NotNull(resources);
            Assert.Empty(resources);
        }

        [Fact]
        public async Task GetResourcesByGroup_VerifiesSeededGroupData()
        {
            // Seed the test DB with the expected application default data
            var client = GetClientWithInMemoryDb(db =>
            {
                if (!db.Groups.Any(g => g.GroupId == 1))
                {
                    db.Groups.Add(new pto.track.data.Models.Group { GroupId = 1, Name = "Group 1" });
                }

                var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);
                db.Resources.Add(new Resource { Id = 1, Name = "Test Employee 1", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP001", Email = "employee@example.com", ActiveDirectoryId = "mock-ad-guid-employee", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 });
                db.Resources.Add(new Resource { Id = 2, Name = "Test Employee 2", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP002", Email = "employee2@example.com", ActiveDirectoryId = "mock-ad-guid-employee2", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 });
                db.Resources.Add(new Resource { Id = 3, Name = "Manager", Role = "Manager", IsActive = true, IsApprover = true, EmployeeNumber = "MGR001", Email = "manager@example.com", ActiveDirectoryId = "mock-ad-guid-manager", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 });
                db.Resources.Add(new Resource { Id = 4, Name = "Approver", Role = "Approver", IsActive = true, IsApprover = true, EmployeeNumber = "APR001", Email = "approver@example.com", ActiveDirectoryId = "mock-ad-guid-approver", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 });
                db.Resources.Add(new Resource { Id = 5, Name = "Administrator", Role = "Admin", IsActive = true, IsApprover = true, EmployeeNumber = "ADMIN001", Email = "admin@example.com", ActiveDirectoryId = "mock-ad-guid-admin", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 });
            });

            // Act - Get resources for Group 1 (seeded group)
            var resp = await client.GetAsync("/api/resources/group/1", GetTimeoutToken());

            // Assert
            resp.EnsureSuccessStatusCode();
            var resources = await resp.Content.ReadFromJsonAsync<List<ResourceDto>>();
            Assert.NotNull(resources);
            // The seed data should include 5 resources in Group 1
            Assert.Equal(5, resources.Count);
            Assert.All(resources, r => Assert.Equal(1, r.GroupId));
        }
    }
}
