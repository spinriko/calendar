using System;
using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.data
{
    /// <summary>
    /// Database context for the PTO tracking application.
    /// </summary>
    public class PtoTrackDbContext : DbContext
    {
        /// <summary>
        /// Gets or sets the collection of scheduled events.
        /// </summary>
        public DbSet<SchedulerEvent> Events { get; set; }

        /// <summary>
        /// Gets or sets the collection of resources (employees).
        /// </summary>
        public DbSet<Resource> Resources { get; set; }


        /// <summary>
        /// Gets or sets the collection of groups.
        /// </summary>
        public DbSet<Models.Group> Groups { get; set; }

        /// <summary>
        /// Gets or sets the collection of absence requests.
        /// </summary>
        public DbSet<AbsenceRequest> AbsenceRequests { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PtoTrackDbContext"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public PtoTrackDbContext(DbContextOptions<PtoTrackDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureEntities(modelBuilder);

            // Skip model-level seeding when running tests so tests can control DB contents.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SeedData(modelBuilder);
        }

        private void ConfigureEntities(ModelBuilder modelBuilder)
        {
            // Configure Resource primary key as identity
            modelBuilder.Entity<Resource>()
                .Property(r => r.Id)
                .ValueGeneratedOnAdd();

            // Configure default values for Resource timestamps
            modelBuilder.Entity<Resource>()
                .Property(r => r.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Resource>()
                .Property(r => r.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

            // Seed Group 1
            modelBuilder.Entity<Models.Group>().HasData(new Models.Group
            {
                GroupId = 1,
                Name = "Group 1"
            });

            // Seed required resources, all assigned to Group 1
            var resources = new[]
            {
                CreateResource(1, "Test Employee 1", "Employee", false, "EMP001", "employee@example.com", "mock-ad-guid-employee", seedDate),
                CreateResource(2, "Test Employee 2", "Employee", false, "EMP002", "employee2@example.com", "mock-ad-guid-employee2", seedDate),
                CreateResource(3, "Manager", "Manager", true, "MGR001", "manager@example.com", "mock-ad-guid-manager", seedDate),
                CreateResource(4, "Approver", "Approver", true, "APR001", "approver@example.com", "mock-ad-guid-approver", seedDate),
                CreateResource(5, "Administrator", "Admin", true, "ADMIN001", "admin@example.com", "mock-ad-guid-admin", seedDate)
            };

            modelBuilder.Entity<Resource>().HasData(resources);
        }

        private Resource CreateResource(int id, string name, string role, bool isApprover, string empNum, string email, string adId, DateTime date)
        {
            return new Resource
            {
                Id = id,
                Name = name,
                Role = role,
                IsActive = true,
                IsApprover = isApprover,
                EmployeeNumber = empNum,
                Email = email,
                ActiveDirectoryId = adId,
                CreatedDate = date,
                ModifiedDate = date,
                GroupId = 1
            };
        }
    }
}
