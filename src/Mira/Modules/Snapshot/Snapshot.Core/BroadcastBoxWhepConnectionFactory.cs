using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;

namespace Snapshot.Core;

public class BroadcastBoxWhepConnectionFactory : IWhepConnectionFactory
{
    private readonly BroadcastBoxHttpClient _httpClient;
    private readonly ILogger<BroadcastBoxWhepConnectionFactory> _logger;

    public BroadcastBoxWhepConnectionFactory(BroadcastBoxHttpClient httpClient, ILogger<BroadcastBoxWhepConnectionFactory> logger, IOptions<SnapshotOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;

        try
        {
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, libPath: options.Value.FfmpegLibPath);
        }
        catch (ApplicationException)
        {
            _logger.LogCritical("[SNAPSHOT] Failed to load FFMPEG binaries from path: {Path}", options.Value.FfmpegLibPath);
            throw;
        }
    }

    public Task<WhepConnection?> CreateAsync(string hostUrl, string streamKey) =>
        CreateAsync(hostUrl, streamKey, CancellationToken.None);

    public async Task<WhepConnection?> CreateAsync(string hostUrl, string streamKey, CancellationToken cancellationToken)
    {
        var peerConnection = new RTCPeerConnection(new RTCConfiguration
        {
            iceServers = [new RTCIceServer { urls = "stun:stun.cloudflare.com:3478" }],
            X_UseRtpFeedbackProfile = true
        });
        
        var videoEndpoint = new FFmpegVideoEndPoint();
        videoEndpoint.RestrictFormats(format => format.Codec == VideoCodecsEnum.H264);
        
        peerConnection.addTrack(new MediaStreamTrack(videoEndpoint.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly));
        peerConnection.OnVideoFormatsNegotiated += formats => videoEndpoint.SetVideoSinkFormat(formats.First());
        peerConnection.OnVideoFrameReceived += videoEndpoint.GotVideoFrame;

        var offer = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);

        var answer = await _httpClient.ExchangeOfferAsync(hostUrl, streamKey, offer.sdp, cancellationToken);
        if (answer is null)
        {
            _logger.LogError("[SNAPSHOT][{Host}][{Key}] Answer was null. Skipping.", hostUrl, streamKey);
            return null;
        }

        var descriptionResponse = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.answer,
            sdp = answer
        });

        if (descriptionResponse != SetDescriptionResultEnum.OK)
        {
            _logger.LogCritical("[SNAPSHOT][{Host}][{Key}] Failed to set remote description. Response: {DescriptionResponse}", hostUrl, streamKey, descriptionResponse);
            return null;
        }

        return new WhepConnection(peerConnection, videoEndpoint);
    }
}