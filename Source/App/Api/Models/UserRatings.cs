namespace MoviesAndTVShowsToDo.Api.Models;

public static class UserRatings
{
    public const int MinScore = 1;
    public const int MaxScore = 10;

    public static void Validate(int rating, string name)
    {
        if (rating is < MinScore or > MaxScore)
            throw new ArgumentOutOfRangeException(name, $"Rating must be between {MinScore} and {MaxScore}.");
    }
}
