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
        public DbSet<SchedulerResource> Resources { get; set; }

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
            // Configure SchedulerResource primary key as identity
            modelBuilder.Entity<SchedulerResource>()
                .Property(r => r.Id)
                .ValueGeneratedOnAdd();

            // Configure default values for Resource timestamps
            modelBuilder.Entity<SchedulerResource>()
                .Property(r => r.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SchedulerResource>()
                .Property(r => r.ModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            // Skip model-level seeding when running tests so tests can control DB contents.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 1,
                Name = "Resource A",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 2,
                Name = "Resource B",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 3,
                Name = "Resource C",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 4,
                Name = "Resource D",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 5,
                Name = "Resource E",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 6,
                Name = "Resource F",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 7,
                Name = "Resource G",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 8,
                Name = "Resource H",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 9,
                Name = "Resource I",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource
            {
                Id = 10,
                Name = "Resource J",
                Role = "Employee",
                IsActive = true,
                IsApprover = false,
                CreatedDate = seedDate,
                ModifiedDate = seedDate
            });

        }
    }
}
