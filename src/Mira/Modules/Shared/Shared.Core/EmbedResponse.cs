using Discord;

namespace Shared.Core;

public static class EmbedResponse
{
    public static Embed Failed(string description) =>
        new EmbedBuilder()
            .WithTitle("Failed")
            .WithDescription(description)
            .WithColor(Color.Red)
            .WithCurrentTimestamp()
            .Build();
    
    public static Embed Success(string description) =>
        new EmbedBuilder()
            .WithTitle("Success")
            .WithDescription(description)
            .WithColor(Color.Green)
            .WithCurrentTimestamp()
            .Build();
}