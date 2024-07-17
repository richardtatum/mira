using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace Mira;

internal class LoggingService(ILogger<Program> logger)
{
    internal Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
        {
            logger.LogWarning("[COMMAND][{Severity}] {Command} failed to execute in {Channel}", message.Severity, cmdException.Command.Aliases[0], cmdException.Context.Channel);
            logger.LogWarning("{Exception}", cmdException.ToString());
            return Task.CompletedTask;
        }
        
        logger.LogWarning("[GENERAL][{Severity}] {Message}", message.Severity, message);
        return Task.CompletedTask;
    }
}