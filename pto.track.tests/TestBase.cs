using System;
using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.tests
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
