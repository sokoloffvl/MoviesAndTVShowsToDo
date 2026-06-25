using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Tests.Models;

[TestFixture]
public class UserRatingsTests
{
    [TestCase(0)]
    [TestCase(11)]
    public void Validate_RejectsOutOfRange(int rating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserRatings.Validate(rating, "Story"));
    }

    [TestCase(1)]
    [TestCase(10)]
    public void Validate_AcceptsValidRange(int rating)
    {
        Assert.DoesNotThrow(() => UserRatings.Validate(rating, "Story"));
    }
}
