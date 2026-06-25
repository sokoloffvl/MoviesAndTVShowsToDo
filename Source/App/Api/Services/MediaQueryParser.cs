using System.Text.RegularExpressions;

using MoviesAndTVShowsToDo.Api.Models;



namespace MoviesAndTVShowsToDo.Api.Services;



public static partial class MediaQueryParser

{

    [GeneratedRegex(@"imdb\.com/title/(tt\d+)", RegexOptions.IgnoreCase)]

    private static partial Regex ImdbUrlPattern();



    public static ParsedMediaQuery Parse(string query)

    {

        var trimmed = query.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))

            return new ParsedMediaQuery(ParsedMediaQueryKind.Empty, null, null);



        var imdbMatch = ImdbUrlPattern().Match(trimmed);

        if (imdbMatch.Success)

            return new ParsedMediaQuery(ParsedMediaQueryKind.ImdbId, imdbMatch.Groups[1].Value, null);



        return new ParsedMediaQuery(ParsedMediaQueryKind.Title, null, trimmed);

    }

}



public enum ParsedMediaQueryKind

{

    Empty,

    ImdbId,

    Title

}



public record ParsedMediaQuery(ParsedMediaQueryKind Kind, string? ImdbId, string? Title);

