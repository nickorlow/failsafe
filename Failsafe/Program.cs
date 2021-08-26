using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
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

            _client.SetGameAsync("Safe from Failing!");
            await Task.Delay(-1);
        }
        
        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
 
    [Group("fs")]
    public class SampleModule : ModuleBase<SocketCommandContext>
    {
        [Command("add")]
        [Summary("Adds an assignment")]
        public async Task SquareAsync(
            [Summary("The date the assignment is due mmddyyyy")] string date, 
            [Summary("The code for the class e.g. CS314")] string courseCode, 
            [Summary("The name of the assignment")] string name)
        {
            try
            {
                if (!int.TryParse(date, out int none)) {
                    await Context.Message.ReplyAsync($"Invalid Date!");
                    return;
                }
                
                DateTime dueDate = new DateTime(int.Parse(date.Substring(4,4)),int.Parse(date.Substring(0,2)), int.Parse(date.Substring(2,2)));
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
                   
                if (dueDate < currentDate)
                {
                    await Context.Message.ReplyAsync($"Assignment was already due!");
                    return;
                }
                
                EmbedBuilder builder = new();

                builder.WithTitle("Successfully added Assignment");
                builder.WithDescription("You will be reminded 1 week, 3 days, and one day before the assignment is due");
                builder.AddField(courseCode, "Course", false); 
                builder.AddField(name, "Assignment Name");
                builder.AddField(dueDate.ToShortDateString(), "Due Date");
                builder.WithColor(Color.Blue);

                await Context.Message.ReplyAsync($"", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }

        
        [Command("addcourse")]
        [Summary("Adds a class ")]
        public async Task UserInfoAsync(string courseName, SocketRole courseRole)
        {
            try
            {
                courseName = courseName.ToUpper();
                EmbedBuilder builder = new();

                builder.WithTitle($"Created Course {courseName}");
                builder.WithDescription("Failsafe bot keeps you from failing your classes.");
                builder.AddField($"@{courseRole.Name}", "Course Role", false); // true - for inline

                builder.WithColor(Color.Blue);

                await Context.Message.ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }
        
        [Command("help")]
        [Summary("Returns usage information about this bot")]
        public async Task SendHelpAsync()
        {
            try
            {
                EmbedBuilder builder = new();

                builder.WithTitle("Help for Failsafe Bot");
                builder.WithDescription("Failsafe bot keeps you from failing your classes.");
                builder.AddField("/fs help", "shows this screen", false); // true - for inline
                builder.AddField("/fs add [date (mmddyyyy)] [course code] [assignment name]", "adds an assignment", false);
                builder.AddField("/fs addcource [course code] [course role]", "adds a course", false);
                builder.AddField("/fs upcoming", "shows your upcoming assignment deadlines", false);

                builder.WithColor(Color.Blue);

                await Context.Message.ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
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



                if (!result.IsSuccess)
                    throw new Exception("Unknown Error");

            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, context);
            }
        }
        
        
    }
}
        