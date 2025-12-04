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

            // Skip model-level seeding when running tests so tests can control DB contents.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

            // Seed Group 1
            modelBuilder.Entity<Models.Group>().HasData(new Models.Group
            {
                GroupId = 1,
                Name = "Group 1"
            });

            // Seed required resources, all assigned to Group 1
            modelBuilder.Entity<Resource>().HasData(new Resource
            {
                Id = 1,
                Name = "Test Employee 1",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                EmployeeNumber = "EMP001",
                Email = "employee@example.com",
                ActiveDirectoryId = "mock-ad-guid-employee",
                CreatedDate = seedDate,
                ModifiedDate = seedDate,
                GroupId = 1
            });
            modelBuilder.Entity<Resource>().HasData(new Resource
            {
                Id = 2,
                Name = "Test Employee 2",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                EmployeeNumber = "EMP002",
                Email = "employee2@example.com",
                ActiveDirectoryId = "mock-ad-guid-employee2",
                CreatedDate = seedDate,
                ModifiedDate = seedDate,
                GroupId = 1
            });
            modelBuilder.Entity<Resource>().HasData(new Resource
            {
                Id = 3,
                Name = "Manager",
                Role = "Manager",
                IsActive = true,
                IsApprover = true,
                EmployeeNumber = "MGR001",
                Email = "manager@example.com",
                ActiveDirectoryId = "mock-ad-guid-manager",
                CreatedDate = seedDate,
                ModifiedDate = seedDate,
                GroupId = 1
            });
            modelBuilder.Entity<Resource>().HasData(new Resource
            {
                Id = 4,
                Name = "Approver",
                Role = "Approver",
                IsActive = true,
                IsApprover = true,
                EmployeeNumber = "APR001",
                Email = "approver@example.com",
                ActiveDirectoryId = "mock-ad-guid-approver",
                CreatedDate = seedDate,
                ModifiedDate = seedDate,
                GroupId = 1
            });
            modelBuilder.Entity<Resource>().HasData(new Resource
            {
                Id = 5,
                Name = "Administrator",
                Role = "Admin",
                IsActive = true,
                IsApprover = true,
                EmployeeNumber = "ADMIN001",
                Email = "admin@example.com",
                ActiveDirectoryId = "mock-ad-guid-admin",
                CreatedDate = seedDate,
                ModifiedDate = seedDate,
                GroupId = 1
            });

        }
    }
}
