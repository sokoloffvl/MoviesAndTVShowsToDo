using System.Threading.Channels;

namespace MoviesAndTVShowsToDo.Api.Services;

public sealed class RecommendationRefreshQueue : IRecommendationRefreshQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public void EnqueueAfterMediaAdded(Guid mediaId) =>
        _channel.Writer.TryWrite(mediaId);
}
