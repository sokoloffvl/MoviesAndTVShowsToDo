using MoviesAndTVShowsToDo.Api.Configuration;

namespace MoviesAndTVShowsToDo.Tests.Configuration;

[TestFixture]
public class DatabaseUrlParserTests
{
    [Test]
    public void Parse_StandardPostgresUrl_BuildsNpgsqlConnectionString()
    {
        const string url = "postgres://postgres:secret@containers-us-west-123.railway.app:6543/railway";

        var result = DatabaseUrlParser.Parse(url);

        Assert.That(result, Does.Contain("Host=containers-us-west-123.railway.app"));
        Assert.That(result, Does.Contain("Port=6543"));
        Assert.That(result, Does.Contain("Database=railway"));
        Assert.That(result, Does.Contain("Username=postgres"));
        Assert.That(result, Does.Contain("Password=secret"));
        Assert.That(result, Does.Contain("SSL Mode=Require"));
    }

    [Test]
    public void Parse_PostgresqlScheme_Works()
    {
        const string url = "postgresql://user:pass@db.example.com:5432/movies_todo";

        var result = DatabaseUrlParser.Parse(url);

        Assert.That(result, Does.Contain("Host=db.example.com"));
        Assert.That(result, Does.Contain("Database=movies_todo"));
    }

    [Test]
    public void Parse_UrlEncodedPassword_DecodesSpecialCharacters()
    {
        const string url = "postgres://postgres:p%40ss%2Fw%3Drd@host:5432/db";

        var result = DatabaseUrlParser.Parse(url);

        Assert.That(result, Does.Contain("Password=p@ss/w=rd"));
    }

    [Test]
    public void FromEnvironment_WhenUnset_ReturnsNull()
    {
        var original = Environment.GetEnvironmentVariable("DATABASE_URL");
        try
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", null);

            Assert.That(DatabaseUrlParser.FromEnvironment(), Is.Null);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", original);
        }
    }

    [Test]
    public void FromEnvironment_WhenSet_ReturnsParsedConnectionString()
    {
        var original = Environment.GetEnvironmentVariable("DATABASE_URL");
        try
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://u:p@host:5432/db");

            var result = DatabaseUrlParser.FromEnvironment();

            Assert.That(result, Does.Contain("Host=host"));
            Assert.That(result, Does.Contain("Username=u"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", original);
        }
    }
}
