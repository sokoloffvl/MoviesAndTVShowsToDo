namespace MoviesAndTVShowsToDo.Api.Services;

public class DuplicateMediaException(string title)
    : InvalidOperationException($"{title} is already on your list.")
{
    public string Title { get; } = title;
}
