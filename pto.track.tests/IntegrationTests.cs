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
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
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

                        // If the test provides a seed action, clear any model-seeded data
                        // so tests control the data state precisely.
                        if (seed != null)
                        {
                            var existingResources = db.Resources.ToList();
                            if (existingResources.Any())
                            {
                                db.Resources.RemoveRange(existingResources);
                            }

                            var existingEvents = db.Events.ToList();
                            if (existingEvents.Any())
                            {
                                db.Events.RemoveRange(existingEvents);
                            }

                            // Persist removals before invoking the test seed
                            db.SaveChanges();
                        }

                        seed?.Invoke(db);
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (ArgumentException)
                        {
                            // Ignore duplicate key errors when seeding during host build
                        }
                    }
                });
            });

            return factory.CreateClient();
        }

        [Fact]
        public async Task GetResources_ReturnsSeededResources()
        {
            var client = GetClientWithInMemoryDb(db =>
            {
                db.Resources.Add(new SchedulerResource { Id = 1, Name = "R1" });
                db.Resources.Add(new SchedulerResource { Id = 2, Name = "R2" });
            });

            var resp = await client.GetAsync("/api/resources");
            resp.EnsureSuccessStatusCode();
            var resources = await resp.Content.ReadFromJsonAsync<List<SchedulerResource>>();
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
            var resp = await client.GetAsync($"/api/events?start={WebUtility.UrlEncode(start)}&end={WebUtility.UrlEncode(end)}");
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

            var postResp = await client.PostAsJsonAsync("/api/events", newEvent);
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
            var created = await postResp.Content.ReadFromJsonAsync<EventDto>();
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);

            var id = created.Id;

            // Read
            var getResp = await client.GetAsync($"/api/events/{id}");
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
            var putResp = await client.PutAsJsonAsync($"/api/events/{id}", updateDto);
            Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

            // Read back updated
            var getResp2 = await client.GetAsync($"/api/events/{id}");
            getResp2.EnsureSuccessStatusCode();
            var fetched2 = await getResp2.Content.ReadFromJsonAsync<EventDto>();
            Assert.NotNull(fetched2);
            Assert.Equal("E2E Updated", fetched2.Text);

            // Delete
            var delResp = await client.DeleteAsync($"/api/events/{id}");
            Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

            // Verify deleted
            var getAfterDel = await client.GetAsync($"/api/events/{id}");
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

            var resp = await client.PostAsJsonAsync("/api/events", badEvent);
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
            var get = await client.GetAsync($"/api/events/{eventId}");
            get.EnsureSuccessStatusCode();
            var ev = await get.Content.ReadFromJsonAsync<SchedulerEvent>();
            Assert.NotNull(ev);

            // Set invalid dates
            ev.Start = new DateTime(2025, 11, 15, 12, 0, 0);
            ev.End = new DateTime(2025, 11, 15, 11, 0, 0);

            var put = await client.PutAsJsonAsync($"/api/events/{ev.Id}", ev);
            Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        }
    }
}
