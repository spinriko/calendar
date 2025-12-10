using System;
using Microsoft.EntityFrameworkCore;

namespace pto.track.data
{
    public static class SeedDefaults
    {
        public static void ApplyModelSeed(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Models.Group>().HasData(new Models.Group
            {
                GroupId = 1,
                Name = "Group 1"
            });

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

        public static void EnsureSeedData(PtoTrackDbContext context)
        {
            // Seed Group 1
            if (!context.Groups.Any(g => g.GroupId == 1))
            {
                context.Groups.Add(new Models.Group { GroupId = 1, Name = "Group 1" });
            }

            var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);
            var resources = new[]
            {
                new Resource { Id = 1, Name = "Test Employee 1", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP001", Email = "employee@example.com", ActiveDirectoryId = "mock-ad-guid-employee", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
                new Resource { Id = 2, Name = "Test Employee 2", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP002", Email = "employee2@example.com", ActiveDirectoryId = "mock-ad-guid-employee2", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
                new Resource { Id = 3, Name = "Manager", Role = "Manager", IsActive = true, IsApprover = true, EmployeeNumber = "MGR001", Email = "manager@example.com", ActiveDirectoryId = "mock-ad-guid-manager", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
                new Resource { Id = 4, Name = "Approver", Role = "Approver", IsActive = true, IsApprover = true, EmployeeNumber = "APR001", Email = "approver@example.com", ActiveDirectoryId = "mock-ad-guid-approver", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
                new Resource { Id = 5, Name = "Administrator", Role = "Admin", IsActive = true, IsApprover = true, EmployeeNumber = "ADMIN001", Email = "admin@example.com", ActiveDirectoryId = "mock-ad-guid-admin", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 }
            };

            foreach (var r in resources)
            {
                if (!context.Resources.Any(e => e.Id == r.Id))
                {
                    context.Resources.Add(r);
                }
            }

            context.SaveChanges();
        }

        private static Resource CreateResource(int id, string name, string role, bool isApprover, string empNum, string email, string adId, DateTime date)
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
