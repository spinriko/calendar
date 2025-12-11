using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.services.DbContextStrategies
{
    public class SqlServerDbContextStrategy : IDbContextStrategy
    {
        private readonly string _connectionString;

        public SqlServerDbContextStrategy(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool IsInMemory => false;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PtoTrackDbContext>(options =>
                options.UseSqlServer(_connectionString, sqlOptions =>
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));
        }
    }
}
