using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Interfaces;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Snapshot.Core;

public class SnapShotService : ISnapshotService
{
    private readonly SnapshotOptions _options;
    private readonly ILogger<SnapShotService> _logger;
    private readonly CommandRepository _commandRepository;
    private readonly HttpClient _httpClient;
    private readonly QueryRepository _queryRepository;
    private List<string> _completedSnapshots = new();
    
    public SnapShotService(IOptions<SnapshotOptions> snapshotOptions, ILogger<SnapShotService> logger, CommandRepository commandRepository, HttpClient httpClient, QueryRepository queryRepository)
    {
        _options = snapshotOptions.Value;
        _logger = logger;
        _commandRepository = commandRepository;
        _httpClient = httpClient;
        _queryRepository = queryRepository;

        try
        {
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, libPath: snapshotOptions.Value.FfmpegLibPath);
        }
        catch (ApplicationException ex)
        {
            _logger.LogCritical("[SNAPSHOT] Failed to load FFMPEG binaries from path: {Path}", snapshotOptions.Value.FfmpegLibPath);
            throw;
        }
    }

    public async Task ExecuteAsync(string hostUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("[SNAPSHOT] Snapshots disabled. Skipping.");
            return;
        }

        var streamKeys = await _queryRepository.GetLiveStreamKeysAsync(hostUrl, cancellationToken);
        _logger.LogInformation("[SNAPSHOT][{Host}] {Count} live stream(s) found. Attempting to obtain snapshots.", hostUrl, streamKeys.Length);
        await Task.WhenAll(streamKeys.Select(key => ExecuteAsync(hostUrl, key, cancellationToken)));
    }

    public async Task ExecuteAsync(string hostUrl, string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Attempting snapshot.", hostUrl, streamKey);
        var peerConnection = await CreatePeerConnection(hostUrl, streamKey, cancellationToken);
        if (peerConnection is null)
        {
            _logger.LogError("[SNAPSHOT][{Host}][{Key}] Failed to create PeerConnection.", hostUrl, streamKey);
            return;
        }

        // Need to add a maximum timeout
        var attempts = 0;
        _completedSnapshots = new();
        await peerConnection.Start();
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection open.", hostUrl, streamKey);
        
        // 10 seconds is a crazy long time to wait for a snapshot. Need to work out how to speed this up
        while (!_completedSnapshots.Contains(streamKey) && attempts < 10)
        {
            attempts++;
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Awaiting snapshot completion. Attempt: {Attempt}", hostUrl, streamKey, attempts);
            await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
        }

        peerConnection.Close("Snapshot completed.");
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection closed.", hostUrl, streamKey);
    }

    private async Task<RTCPeerConnection?> CreatePeerConnection(string hostUrl, string streamKey, CancellationToken cancellationToken)
    {
        var videoEndpoint = new FFmpegVideoEndPoint();
        videoEndpoint.RestrictFormats(format => format.Codec == VideoCodecsEnum.H264);

        videoEndpoint.OnVideoSinkDecodedSampleFaster += async image =>
        {
            await SaveFrameToDatabaseAsync(streamKey, image.Sample, image.Width, image.Height, image.Stride, cancellationToken);
            _completedSnapshots.Add(streamKey);
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Snapshot saved successfully", hostUrl, streamKey);
        };

        var peerConnection = new RTCPeerConnection(new RTCConfiguration
        {
            iceServers = [new RTCIceServer { urls = "stun:stun.cloudflare.com:3478" }],
            X_UseRtpFeedbackProfile = true
        });
        
        peerConnection.addTrack(new MediaStreamTrack(videoEndpoint.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly));
        peerConnection.OnVideoFormatsNegotiated += formats => videoEndpoint.SetVideoSinkFormat(formats.First());
        peerConnection.OnVideoFrameReceived += videoEndpoint.GotVideoFrame;

        var offer = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);

        var answer = await SendOfferAsync(hostUrl, offer.sdp, streamKey, cancellationToken);
        if (answer is null)
        {
            _logger.LogError("[SNAPSHOT][{Host}][{Key}] Answer was null. Skipping.", hostUrl, streamKey);
            _completedSnapshots.Add(streamKey);
            return null;
        }

        var descriptionResponse = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.answer,
            sdp = answer
        });

        if (descriptionResponse != SetDescriptionResultEnum.OK)
        {
            _completedSnapshots.Add(streamKey);
            _logger.LogCritical("[SNAPSHOT][{Host}][{Key}] Failed to set remote description. Response: {DescriptionResponse}", hostUrl, streamKey, descriptionResponse);
            return null;
        }

        return peerConnection;
    }

    private async Task<string?> SendOfferAsync(string hostUrl, string offerSdp, string streamKey, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", streamKey);
        // This can probably be added where the client is registered
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sdp"));

        var content = new StringContent(offerSdp, Encoding.UTF8, "application/sdp");

        hostUrl = $"{hostUrl}/api/whep";
        var response = await _httpClient.PostAsync(hostUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogCritical("[SNAPSHOT][{Host}][{Key}] Offer request failed. Status: {Status}, Reason: {Reason}", hostUrl, streamKey, response.StatusCode, response.ReasonPhrase);
            return null;
        }

        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Offer request succeeded.", hostUrl, streamKey);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task SaveFrameToDatabaseAsync(string streamKey, IntPtr ptr, int width, int height, int stride,
        CancellationToken cancellationToken)
    {
        var bytes = await ExtractFrameAsync(ptr, width, height, stride, cancellationToken);
        await _commandRepository.SaveSnapshotAsync(streamKey, bytes, cancellationToken);
    }
    
    private async Task<byte[]> ExtractFrameAsync(IntPtr ptr, int width, int height, int stride, CancellationToken cancellationToken)
    {
        var bytesPerPixel = 3; // Assuming Rgba24 format
        var totalBytes = height * stride; // Calculate total bytes in the image
        var pixelData = new byte[totalBytes];

        Marshal.Copy(ptr, pixelData, 0, totalBytes);

        // The data is in BGR format it needs to be converted to RGB
        pixelData = SwapRedBlueChannels(width, height, stride, bytesPerPixel, pixelData);

        // Create ImageSharp image from pixel data
        var image = Image.LoadPixelData<Rgb24>(pixelData, width, height);

        using var stream = new MemoryStream();
        await image.SaveAsWebpAsync(stream, new WebpEncoder
        {
            Quality = _options.QualityLevel
        }, cancellationToken);

        return stream.ToArray();
    }
    
    private static byte[] SwapRedBlueChannels(int width, int height, int stride, int bytesPerPixel, byte[] pixelData)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * stride + x * bytesPerPixel;

                // Swap BGR to RGB
                (pixelData[index], pixelData[index + 2]) = (pixelData[index + 2], pixelData[index]);
            }
        }

        return pixelData;
    }
}