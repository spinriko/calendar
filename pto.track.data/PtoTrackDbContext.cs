using System;
using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.data
{
    public class PtoTrackDbContext : DbContext
    {
        public DbSet<SchedulerEvent> Events { get; set; }
        public DbSet<SchedulerResource> Resources { get; set; }
        public DbSet<AbsenceRequest> AbsenceRequests { get; set; }

        public PtoTrackDbContext(DbContextOptions<PtoTrackDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Skip model-level seeding when running tests so tests can control DB contents.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 1, Name = "Resource A" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 2, Name = "Resource B" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 3, Name = "Resource C" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 4, Name = "Resource D" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 5, Name = "Resource E" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 6, Name = "Resource F" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 7, Name = "Resource G" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 8, Name = "Resource H" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 9, Name = "Resource I" });
            modelBuilder.Entity<SchedulerResource>().HasData(new SchedulerResource { Id = 10, Name = "Resource J" });

        }
    }
}
