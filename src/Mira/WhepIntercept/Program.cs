using System.Net.Http.Headers;
using System.Text;
using System.Threading.Channels;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using FFmpeg.NET;

var snapshotCounter = 0;
var cancellationTokenSource = new CancellationTokenSource();

var channel = Channel.CreateUnbounded<(byte[], uint)>();

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

while (await channel.Reader.WaitToReadAsync())
{
    while (channel.Reader.TryRead(out (byte[] bytes, uint timestamp) h264Frame))
    {
        await SaveFrameToFile(h264Frame.bytes, h264Frame.timestamp);
    }
}


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

    // Initialize the PeerConnection.
    var pc = new RTCPeerConnection(config);
    pc.addTrack(new MediaStreamTrack(new VideoFormat(VideoCodecsEnum.H264, 126),
        MediaStreamStatusEnum.RecvOnly));

    pc.onconnectionstatechange += (state) => { Console.WriteLine($"State changed: {state}"); };

    pc.oniceconnectionstatechange += (state) =>
    {
        Console.WriteLine($"ICE connection state changed: {state}");
    };

    pc.OnSendReport += (media, sr) =>
    {
        Console.WriteLine($"RTCP Send for {media} \n {sr.GetDebugSummary()}");
    };

    pc.OnVideoFormatsNegotiated += list =>
    {
        Console.WriteLine($"Formats received: {string.Join(',', list.Select(x => x.Codec))}");
    };

    // pc.OnVideoFrameReceived += async (point, u, bytes, format) =>
    // {
    //     await SaveFrameToFile(bytes);
    //     Console.WriteLine($"Frame received. Format: {format.Codec}");
    // };

    pc.OnVideoFrameReceivedByIndex += (i, point, timestamp, bytes, format) =>
    {
        Console.WriteLine($"[FRAME]: TS: {timestamp}, A: {point.Address} {point.Port}, F: {format.Codec} {format.Parameters} {format.FormatID} {format.ClockRate} {format.FormatName} ");
        channel.Writer.TryWrite((bytes, timestamp));
    };
    
    return pc;
}

// static unsafe Image<Rgb24> DecodeH264ToImage(byte[] h264Frame)
// {
// // Find the decoder for the H.264 codec
//     AVCodec* codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
//     if (codec == null)
//     {
//         Console.WriteLine("Codec not found.");
//         return null;
//     }
//
//     // Allocate video codec context
//     AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
//     if (codecContext == null)
//     {
//         Console.WriteLine("Could not allocate video codec context.");
//         return null;
//     }
//
//     // Open the codec
//     if (ffmpeg.avcodec_open2(codecContext, codec, null) < 0)
//     {
//         Console.WriteLine("Could not open codec.");
//         return null;
//     }
//
//     // Allocate a packet
//     AVPacket* packet = ffmpeg.av_packet_alloc();
//     ffmpeg.av_init_packet(packet);
//     packet->data = (byte*)ffmpeg.av_malloc((ulong)h264Frame.Length);
//     packet->size = h264Frame.Length;
//     Marshal.Copy(h264Frame, 0, (IntPtr)packet->data, h264Frame.Length);
//
//     // Allocate a frame for decoding
//     AVFrame* frame = ffmpeg.av_frame_alloc();
//     if (frame == null)
//     {
//         Console.WriteLine("Could not allocate video frame.");
//         return null;
//     }
//
//     // Allocate a frame for RGB conversion
//     AVFrame* rgbFrame = ffmpeg.av_frame_alloc();
//     if (rgbFrame == null)
//     {
//         Console.WriteLine("Could not allocate RGB frame.");
//         return null;
//     }
//
//     // Send the packet for decoding
//     int result = ffmpeg.avcodec_send_packet(codecContext, packet);
//     if (result < 0)
//     {
//         Console.WriteLine($"Error sending packet for decoding: {result}");
//         return null;
//     }
//
//     // Receive the frame from the decoder
//     result = ffmpeg.avcodec_receive_frame(codecContext, frame);
//     if (result < 0)
//     {
//         Console.WriteLine($"Error receiving frame from decoder: {result}");
//         return null;
//     }
//
//     // Set up the RGB frame buffer
//     int width = codecContext->width;
//     int height = codecContext->height;
//     int numBytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, width, height, 1);
//     byte* buffer = (byte*)ffmpeg.av_malloc((ulong)numBytes);
//
//     // Use fixed to pin the memory location for the data and linesize arrays
//     fixed (byte** rgbFrameData = rgbFrame->data)
//     fixed (int* rgbFrameLineSize = rgbFrame->linesize)
//     {
//         // Fill the RGB frame with data
//         int fillResult = ffmpeg.av_image_fill_arrays(rgbFrameData, rgbFrameLineSize, buffer,
//             AVPixelFormat.AV_PIX_FMT_RGB24, width, height, 1);
//         if (fillResult < 0)
//         {
//             Console.WriteLine("Could not fill RGB image arrays.");
//             return null;
//         }
//     }
//
//     // Set up the SwsContext for conversion
//     SwsContext* swsCtx = ffmpeg.sws_getContext(
//         width, height, codecContext->pix_fmt,
//         width, height, AVPixelFormat.AV_PIX_FMT_RGB24,
//         ffmpeg.SWS_BILINEAR, null, null, null);
//
//     if (swsCtx == null)
//     {
//         Console.WriteLine("Could not initialize the SWS context for image conversion.");
//         return null;
//     }
//
//     // Convert the image from its native format to RGB
//     ffmpeg.sws_scale(swsCtx, frame->data, frame->linesize, 0, height, rgbFrame->data, rgbFrame->linesize);
//
//     // Create an ImageSharp image from the raw RGB data
//     var image = Image.LoadPixelData<Rgb24>(buffer, width, height);
//
//     // Free allocated memory
//     ffmpeg.av_free(buffer);
//     ffmpeg.av_frame_free(&frame);
//     ffmpeg.av_frame_free(&rgbFrame);
//     ffmpeg.sws_freeContext(swsCtx);
//     ffmpeg.avcodec_free_context(&codecContext);
//     ffmpeg.av_packet_free(&packet);
//
//     return image;
// }


static async Task SaveFrameToFile(byte[] h264Frame, uint timestamp)
{

    // Create a temporary file to store the H.264 frame
    string tempH264File = "/home/rt/Pictures/temp.h264";
    await File.WriteAllBytesAsync(tempH264File, h264Frame);

    // Output JPEG file path
    string outputJpegFile = $"/home/rt/Pictures/mira/frame-{timestamp}.jpg";

    // Create an instance of Engine
    var ffmpeg = new Engine("/sbin/ffmpeg"); // Adjust the path if necessary

    ffmpeg.Error += (sender, args) =>
    {
        Console.WriteLine($"[ERROR]: {args.Exception.Message}");
    };

    ffmpeg.Complete += (sender, args) =>
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCCESS] {outputJpegFile}");
        Console.ResetColor();
        File.Delete(tempH264File);
    };
    // Create input and output objects
    var inputFile = new InputFile(tempH264File);
    var outputFile = new OutputFile(outputJpegFile);

    // Convert H.264 to JPEG
    await ffmpeg.ConvertAsync(inputFile, outputFile, new CancellationToken());
}