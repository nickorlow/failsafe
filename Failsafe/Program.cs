using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Failsafe
{
    public class Program
    {
        private DiscordSocketClient _client;
      
        
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Sets up and initializes the database, command service, command handler, and Discord client
        /// </summary>
        private async Task MainAsync()
        {
            DatabaseService.Initialize();
            
            _client = new DiscordSocketClient();
            _client.Log += Log;

            var token = Environment.GetEnvironmentVariable("DISCORD_API_KEY");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            var commandService = new CommandService();
            
            // this should register the commands with Discord's fancy new slash commands but it doesn't seem to be working
            // the C# Discord API gets pretty second-rate support. Likely will port to something else when I have more time.
            await commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), null);
            
            var commandHandler = new CommandHandler(_client, commandService);
            await commandHandler.InstallCommandsAsync();

            
            _client.Connected += () =>
            {
                return AlertService.ExecuteAsync(new CancellationToken(), _client);
            };
            
            _client.SetActivityAsync(new Game("Failing Safely"));
            await Task.Delay(-1);
        }
        
        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
 
   
    
    // Code below was mostly taken from documentation
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('/', ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);
            
            try
            {
                // Execute the command with the command context we just
                // created, along with the service provider for precondition checks.
                IResult result =  await _commands.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: null);

                
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    throw new Exception($"Unknown Error: {result.ErrorReason} (Code {result.Error})");
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, context);
            }
        }
        
        
    }
}
        