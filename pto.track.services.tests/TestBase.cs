using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.services.tests;

public class TestBase
{
    protected PtoTrackDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PtoTrackDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PtoTrackDbContext(options);
    }
}
