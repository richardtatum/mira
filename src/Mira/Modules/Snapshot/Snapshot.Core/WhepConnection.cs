using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;

namespace Snapshot.Core;

public class WhepConnection(RTCPeerConnection peerConnection, FFmpegVideoEndPoint videoEndPoint) : IDisposable
{
    public event Action? Disposed; 
    public event VideoSinkSampleDecodedFasterDelegate? FrameReceived
    {
        add => videoEndPoint.OnVideoSinkDecodedSampleFaster += value;
        remove => videoEndPoint.OnVideoSinkDecodedSampleFaster -= value;
    }

    public Task StartAsync() => peerConnection.Start();

    public void Dispose()
    {
        Disposed?.Invoke();
        peerConnection.Dispose();
        videoEndPoint.Dispose();
    }
}