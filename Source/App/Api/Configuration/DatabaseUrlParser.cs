namespace MoviesAndTVShowsToDo.Api.Configuration;

public static class DatabaseUrlParser
{
    public static string? FromEnvironment()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        return string.IsNullOrWhiteSpace(databaseUrl) ? null : Parse(databaseUrl);
    }

    public static string Parse(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
