using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using pto.track.data;

namespace pto.track.services.DbContextStrategies
{
    public class InMemoryDbContextStrategy : IDbContextStrategy
    {
        private readonly string _dbName;
        private readonly InMemoryDatabaseRoot? _root;

        public InMemoryDbContextStrategy(string dbName = "PtoTrack_Testing", InMemoryDatabaseRoot? root = null)
        {
            _dbName = dbName;
            _root = root;
        }

        public bool IsInMemory => true;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PtoTrackDbContext>(options =>
            {
                if (_root != null)
                {
                    options.UseInMemoryDatabase(_dbName, _root);
                }
                else
                {
                    options.UseInMemoryDatabase(_dbName);
                }
            });
        }
    }
}
