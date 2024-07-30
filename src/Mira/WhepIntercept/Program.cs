using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
var cancellationTokenSource = new CancellationTokenSource();


var url = "https://b.siobud.com/api/whep";
var bearerToken = "tatumkhamun-test";

// Create the offer and set it as the local description
var peerConnection = await GetPeerConnection();
var offer = peerConnection.createOffer();
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

Console.WriteLine($"Remote Description Response: {result}");
await peerConnection.Start();


// TODO: Add ICE Candidate??
// TODO: URl from the headers??

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

async Task<RTCPeerConnection> GetPeerConnection()
{
    // Create a new PeerConnection configuration.
    var config = new RTCConfiguration
    {
        iceServers = [new RTCIceServer { urls = "stun:stun.cloudflare.com:3478" },],
        X_UseRtpFeedbackProfile = true
    };

    // FFmpegInit.Initialise(libPath: "/usr/lib64");
    FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, libPath: "/usr/lib64");
    var videoEndpoint = new FFmpegVideoEndPoint();
    
    videoEndpoint.RestrictFormats(format => format.Codec == VideoCodecsEnum.H264);
    var saveFrame = false;
    var saveTimer = new System.Timers.Timer(30000);
    saveTimer.Elapsed += (sender, eventArgs) => saveFrame = true;
    saveTimer.AutoReset = true;
    saveTimer.Enabled = true;
    
    videoEndpoint.OnVideoSinkDecodedSampleFaster += async (image) =>
    {
        if (!saveFrame)
        {
            return;
        }

        Console.WriteLine($"[IMAGE-FAST] Received H:{image.Height} W:{image.Width} F: {image.PixelFormat}");
        SaveImageToFile(image.Sample, image.Width, image.Height, image.Stride, bearerToken);
        saveFrame = false;
    };
    
    // Initialize the PeerConnection.
    var pc = new RTCPeerConnection(config);
    pc.addTrack(new MediaStreamTrack(videoEndpoint.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly));

    pc.onconnectionstatechange += (state) => { Console.WriteLine($"State changed: {state}"); };
    pc.oniceconnectionstatechange += (state) =>
    {
        Console.WriteLine($"ICE connection state changed: {state}");
    };
    
    pc.OnVideoFrameReceived += videoEndpoint.GotVideoFrame;
    pc.OnVideoFormatsNegotiated += (formats) => videoEndpoint.SetVideoSinkFormat(formats.First());
    
    return pc;
}

static void SaveImageToFile(IntPtr ptr, int width, int height, int stride, string streamKey)
{
    // Calculate total bytes in the image
    int bytesPerPixel = 3; // Assuming Rgba24 format
    int totalBytes = height * stride;

    // Copy pixel data from unmanaged memory to managed memory
    byte[] pixelData = new byte[totalBytes];
        
    Marshal.Copy(ptr, pixelData, 0, totalBytes);
    
    // Check if the data is in BGR format and convert to RGB if necessary
    // Assuming BGR format for this example
    pixelData = SwapRedBlueChannels(width, height, stride, bytesPerPixel, pixelData);

    // Create ImageSharp image from pixel data
    var image =  Image.LoadPixelData<Rgb24>(pixelData, width, height);
    
    // Configure encoding options if needed
    var options = new JpegEncoder
    {
        Quality = 90 // Quality can be adjusted between 0 (lowest) and 100 (highest)
    };
    
    
    // Save the image to the specified file path as a JPEG
    image.SaveAsJpeg($"/home/rt/Pictures/mira/frame-{streamKey}.jpg", options);
}

static byte[] SwapRedBlueChannels(int width, int height, int stride, int bytesPerPixel, byte[] pixelData)
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