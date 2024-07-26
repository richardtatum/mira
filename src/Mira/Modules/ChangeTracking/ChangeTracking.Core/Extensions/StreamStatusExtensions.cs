using ChangeTracking.Core.Models;
using Shared.Core;

namespace ChangeTracking.Core.Extensions;

internal static class StreamStatusExtensions
{
    internal static StreamStatus ToStreamStatus(this DetailedStreamStatus status) => status switch
    {
        DetailedStreamStatus.Starting or DetailedStreamStatus.Live => StreamStatus.Live,
        DetailedStreamStatus.Ending or DetailedStreamStatus.Offline => StreamStatus.Offline,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    internal static DetailedStreamStatus ToDetailedStreamStatus(this StreamStatus? status, bool streamCurrentlyLive) =>
        status switch
        {
            StreamStatus.Offline or null when streamCurrentlyLive => DetailedStreamStatus.Starting,
            StreamStatus.Live when streamCurrentlyLive => DetailedStreamStatus.Live,
            StreamStatus.Live when !streamCurrentlyLive => DetailedStreamStatus.Ending,
            StreamStatus.Offline or null when !streamCurrentlyLive => DetailedStreamStatus.Offline,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}