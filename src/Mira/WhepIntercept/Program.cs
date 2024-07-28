using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;

var rawSDPOffer = @"v=0
o=mozilla...THIS_IS_SDPARTA-99.0 8833763482153536527 0 IN IP4 0.0.0.0
s=-
t=0 0
a=fingerprint:sha-256 8C:A3:64:D8:14:77:F7:73:B8:3E:7B:5A:7D:E4:4A:CC:DA:E6:16:8E:4A:D0:80:9E:54:4A:A5:C5:CB:0F:29:F1
a=group:BUNDLE 0 1
a=ice-options:trickle
a=msid-semantic:WMS *
m=audio 9 UDP/TLS/RTP/SAVPF 109 9 0 8 101
c=IN IP4 0.0.0.0
a=recvonly
a=extmap:1 urn:ietf:params:rtp-hdrext:ssrc-audio-level
a=extmap:2/recvonly urn:ietf:params:rtp-hdrext:csrc-audio-level
a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid
a=fmtp:109 maxplaybackrate=48000;stereo=1;useinbandfec=1;stereo=1
a=fmtp:101 0-15
a=ice-pwd:4004efda41b34c10e03993e414163889
a=ice-ufrag:145f5126
a=mid:0
a=rtcp-mux
a=rtpmap:109 opus/48000/2
a=rtpmap:9 G722/8000/1
a=rtpmap:0 PCMU/8000
a=rtpmap:8 PCMA/8000
a=rtpmap:101 telephone-event/8000/1
a=setup:actpass
a=ssrc:2829468867 cname:{dbe7571e-d4ba-40ad-a6c1-052ba561bb9c}
m=video 9 UDP/TLS/RTP/SAVPF 120 124 121 125 126 127 97 98 123 122 119
c=IN IP4 0.0.0.0
a=recvonly
a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid
a=extmap:4 http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time
a=extmap:5 urn:ietf:params:rtp-hdrext:toffset
a=extmap:6/recvonly http://www.webrtc.org/experiments/rtp-hdrext/playout-delay
a=extmap:7 http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01
a=fmtp:126 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1
a=fmtp:97 profile-level-id=42e01f;level-asymmetry-allowed=1
a=fmtp:120 max-fs=12288;max-fr=60
a=fmtp:124 apt=120
a=fmtp:121 max-fs=12288;max-fr=60
a=fmtp:125 apt=121
a=fmtp:127 apt=126
a=fmtp:98 apt=97
a=fmtp:119 apt=122
a=ice-pwd:4004efda41b34c10e03993e414163889
a=ice-ufrag:145f5126
a=mid:1
a=rtcp-fb:120 nack
a=rtcp-fb:120 nack pli
a=rtcp-fb:120 ccm fir
a=rtcp-fb:120 goog-remb
a=rtcp-fb:120 transport-cc
a=rtcp-fb:121 nack
a=rtcp-fb:121 nack pli
a=rtcp-fb:121 ccm fir
a=rtcp-fb:121 goog-remb
a=rtcp-fb:121 transport-cc
a=rtcp-fb:126 nack
a=rtcp-fb:126 nack pli
a=rtcp-fb:126 ccm fir
a=rtcp-fb:126 goog-remb
a=rtcp-fb:126 transport-cc
a=rtcp-fb:97 nack
a=rtcp-fb:97 nack pli
a=rtcp-fb:97 ccm fir
a=rtcp-fb:97 goog-remb
a=rtcp-fb:97 transport-cc
a=rtcp-fb:123 nack
a=rtcp-fb:123 nack pli
a=rtcp-fb:123 ccm fir
a=rtcp-fb:123 goog-remb
a=rtcp-fb:123 transport-cc
a=rtcp-fb:122 nack
a=rtcp-fb:122 nack pli
a=rtcp-fb:122 ccm fir
a=rtcp-fb:122 goog-remb
a=rtcp-fb:122 transport-cc
a=rtcp-mux
a=rtcp-rsize
a=rtpmap:120 VP8/90000
a=rtpmap:124 rtx/90000
a=rtpmap:121 VP9/90000
a=rtpmap:125 rtx/90000
a=rtpmap:126 H264/90000
a=rtpmap:127 rtx/90000
a=rtpmap:97 H264/90000
a=rtpmap:98 rtx/90000
a=rtpmap:123 ulpfec/90000
a=rtpmap:122 red/90000
a=rtpmap:119 rtx/90000
a=setup:actpass
a=ssrc:3969493331 cname:{dbe7571e-d4ba-40ad-a6c1-052ba561bb9c}
";
var snapshotCounter = 0;
var cancellationTokenSource = new CancellationTokenSource();

var url = "https://b.siobud.com/api/whep";
var bearerToken = "tatumkhamun-test";


// Create the offer and set it as the local description
var peerConnection = GetPeerConnection();
var offer = peerConnection.createOffer();
offer.sdp = rawSDPOffer;
await peerConnection.setLocalDescription(offer);

// Request the answer and set it as the remote description
Console.WriteLine("Connecting to WHEP endpoint...");
var answer = await SendOfferToWHEP(url, offer.sdp, bearerToken);
if (answer is null)
{
    await cancellationTokenSource.CancelAsync();
    return;
}

var result = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
{
    type = RTCSdpType.answer, sdp = answer
});

await peerConnection.Start();

// Keep the application running.
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

await cancellationTokenSource.CancelAsync();
return;

static async Task<string?> SendOfferToWHEP(string url, string offerSdp, string bearerToken)
{
    var content = new StringContent(offerSdp, Encoding.UTF8, "application/sdp");

    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sdp"));

    var response = await client.PostAsync(url, content);
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(
            $"Failed to receive answer SDP. StatusCode: {response.StatusCode}, Reason: {response.ReasonPhrase}");
        return null;
    }

    Console.WriteLine($"Received answer SDP");
    var answer = await response.Content.ReadAsStringAsync();
    return answer;
}

static RTCPeerConnection GetPeerConnection()
{
    // Create a new PeerConnection configuration.
    var config = new RTCConfiguration
    {
        // iceServers = [new RTCIceServer { urls = "stun:stun.cloudflare.com:3478" }],
        X_UseRtpFeedbackProfile = true
    };

// Initialize the PeerConnection.
    var peerConnection = new RTCPeerConnection(config);

    peerConnection.onconnectionstatechange += (state) =>
    {
        Console.WriteLine($"State changed: {state}");
    };

    peerConnection.oniceconnectionstatechange += (state) =>
    {
        Console.WriteLine($"ICE connection state changed: {state}");
    };

    peerConnection.OnSendReport += (media, sr) =>
    {
        Console.WriteLine($"RTCP Send for {media} \n {sr.GetDebugSummary()}");
    };

    return peerConnection;
}