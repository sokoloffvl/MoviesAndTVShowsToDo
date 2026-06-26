namespace MoviesAndTVShowsToDo.Api.Models;

public class SimilarSource
{
    public Guid SourceMediaId { get; set; }
    public string SourceTitle { get; set; } = string.Empty;
}
