using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class MediaQueryParserTests
{
    [Test]
    public void Parse_ImdbUrl_ExtractsId()
    {
        var result = MediaQueryParser.Parse("https://www.imdb.com/title/tt1160419/");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ParsedMediaQueryKind.ImdbId));
            Assert.That(result.ImdbId, Is.EqualTo("tt1160419"));
        });
    }

    [Test]
    public void Parse_Title_ReturnsTitleKind()
    {
        var result = MediaQueryParser.Parse("  Dune  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ParsedMediaQueryKind.Title));
            Assert.That(result.Title, Is.EqualTo("Dune"));
        });
    }

    [Test]
    public void Parse_Empty_ReturnsEmptyKind()
    {
        var result = MediaQueryParser.Parse("   ");

        Assert.That(result.Kind, Is.EqualTo(ParsedMediaQueryKind.Empty));
    }
}
