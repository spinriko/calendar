using System;
using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Tests
{
    public abstract class TestBase
    {
        protected SchedulerDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new SchedulerDbContext(options);
        }
    }
}
