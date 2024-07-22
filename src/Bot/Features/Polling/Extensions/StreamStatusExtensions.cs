using Mira.Features.Polling.Models;
using Mira.Features.Shared;

namespace Mira.Features.Polling.Extensions;

public static class StreamStatusExtensions
{
    public static StreamStatus ToStreamStatus(this DetailedStreamStatus status) => status switch
    {
        DetailedStreamStatus.Starting or DetailedStreamStatus.Live => StreamStatus.Live,
        DetailedStreamStatus.Ending or DetailedStreamStatus.Offline => StreamStatus.Offline,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static DetailedStreamStatus ToDetailedStreamStatus(this StreamStatus? status, bool streamCurrentlyLive) =>
        status switch
        {
            StreamStatus.Offline or null when streamCurrentlyLive => DetailedStreamStatus.Starting,
            StreamStatus.Live when streamCurrentlyLive => DetailedStreamStatus.Live,
            StreamStatus.Live when !streamCurrentlyLive => DetailedStreamStatus.Ending,
            StreamStatus.Offline or null when !streamCurrentlyLive => DetailedStreamStatus.Offline,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}