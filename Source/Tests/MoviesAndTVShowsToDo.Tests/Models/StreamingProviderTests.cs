using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Tests.Models;

[TestFixture]
public class StreamingProviderTests
{
    [TestCase(8, StreamingProvider.Netflix)]
    [TestCase(9, StreamingProvider.AmazonPrime)]
    [TestCase(337, StreamingProvider.DisneyPlus)]
    [TestCase(99999, StreamingProvider.Other)]
    public void FromTmdbId_MapsKnownProviders(int tmdbId, StreamingProvider expected)
    {
        Assert.That(StreamingProviderExtensions.FromTmdbId(tmdbId), Is.EqualTo(expected));
    }

    [Test]
    public void ToDisplayName_ReturnsFriendlyNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StreamingProvider.Netflix.ToDisplayName(), Is.EqualTo("Netflix"));
            Assert.That(StreamingProvider.HboMax.ToDisplayName(), Is.EqualTo("Max"));
            Assert.That(StreamingProvider.Other.ToDisplayName(), Is.EqualTo("Other"));
        });
    }
}
