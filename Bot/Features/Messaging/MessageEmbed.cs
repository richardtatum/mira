using Discord;

namespace Mira.Features.Messaging;

public static class MessageEmbed
{
    public static Embed Live(string url, int viewers, TimeSpan duration)
        => new EmbedBuilder()
            .WithTitle("Stream is online")
            .WithDescription($"{url} is currently live!")
            .WithColor(Color.Green)
            .WithFields(
                new EmbedFieldBuilder().WithName("Viewers").WithValue(viewers),
                new EmbedFieldBuilder().WithName("Duration").WithValue(duration.ToString(@"hh\:mm"))
            )
            .WithFooter("Last Updated: ")
            .WithCurrentTimestamp()// Updating the embed means that the stream started with localized time doesn't work
            .Build();

    // I don't think viewers is good for offline. It should be a summary of the stream instead
    // Started, ended, duration, etc
    public static Embed Offline(string url, int viewers, TimeSpan duration)
        => new EmbedBuilder()
            .WithTitle("Stream is offline")            
            .WithDescription($"{url} is now offline.")
            .WithColor(Color.Red)
            .WithFields(
                new EmbedFieldBuilder().WithName("Viewers").WithValue(viewers),
                new EmbedFieldBuilder().WithName("Duration").WithValue(duration.ToString(@"hh\:mm"))
            )
            .WithFooter("Ended: ")
            .WithCurrentTimestamp() 
            .Build();
}