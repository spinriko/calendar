using System;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using pto.track.data;
using pto.track.services.Mapping;

namespace pto.track.tests
{
    public class TestBase
    {
        protected PtoTrackDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PtoTrackDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new PtoTrackDbContext(options);
        }

        protected ILogger<T> CreateLogger<T>()
        {
            return NullLogger<T>.Instance;
        }

        protected IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AbsenceMappingProfile>();
                cfg.AddProfile<EventMappingProfile>();
                cfg.AddProfile<ResourceMappingProfile>();
            });
            return config.CreateMapper();
        }
    }
}
