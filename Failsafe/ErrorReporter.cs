using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Failsafe
{
    public static class ErrorReporter
    {
        public static async Task ReportErrorAsync(Exception ex, SocketCommandContext context)
        {
            DiscordSocketClient client = context.Client;
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Failsafe Bot Error Report");
            builder.WithDescription("Failsafe bot has encountered a critical error.");
            builder.AddField(context.Guild.Id.ToString(), "Guild ID", false);
            builder.AddField(context.Message.Content, "Command", false);
            builder.AddField(ex.StackTrace, "Stack Trace", false);
            builder.AddField(ex.Message, "Exception Message", false);

            builder.WithColor(Color.Red);
                
            await client.GetUser(397223060446511114).SendMessageAsync($"", false, builder.Build());
        }
        
    }
}