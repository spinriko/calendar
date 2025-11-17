using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.services.tests;

public class TestBase
{
    protected SchedulerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SchedulerDbContext(options);
    }
}
