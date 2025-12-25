using System.Threading;
using System.Threading.Tasks;
using pto.track.services.Identity;
using Xunit;

namespace pto.track.services.tests
{
    public class NoOpIdentityEnricherTests
    {
        [Fact]
        public async Task NoOpIdentityEnricher_ReturnsEmptyDictionary()
        {
            var enricher = new NoOpIdentityEnricher();
            var result = await enricher.EnrichAsync("someuser", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
