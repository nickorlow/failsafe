using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DSharpPlus;
using LiteDB;
using TokenType = Discord.TokenType;

namespace Failsafe
{
    public class AlertService
    {
        public static async Task ExecuteAsync(CancellationToken stoppingToken, DiscordSocketClient client)
        {
            do
            {
                try
                {
                    int hour = 20;
                    
                    // We'll send out alerts at 3 P.M. CST / 2 P.M. CDT / 8 P.M. UTC
                    
                    DateTime future = DateTime.UtcNow;
                    if (future.Hour < hour)
                        future = future.AddHours(hour - future.Hour);
                    else
                        future = future.AddHours(hour + 24 - future.Hour);

                    Console.WriteLine((future)-DateTime.UtcNow);

                    await SendAlerts(client);
                  
                    await Task.Delay((future)-DateTime.UtcNow, stoppingToken);
                }
                catch (Exception e)
                {
                    //TODO: Figure out how to report this
                    Console.WriteLine(e);
                    throw;
                }
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        public static async Task SendAlerts(DiscordSocketClient client)
        {
            DateTime checkingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));

            List<Assignment> oneDayAssignments = DatabaseService.AssignmentCollection.Find(x =>
                (checkingTime.AddDays(1) > x.DueDate && checkingTime < x.DueDate)).ToList();
                    
            List<Assignment> threeDayAssignments = DatabaseService.AssignmentCollection.Find(x =>
                (checkingTime.AddDays(3) > x.DueDate && checkingTime.AddDays(2) < x.DueDate)).ToList();
                    
            List<Assignment> sevenDayAssignments = DatabaseService.AssignmentCollection.Find(x =>
                (checkingTime.AddDays(7) > x.DueDate && checkingTime.AddDays(6) < x.DueDate)).ToList();

            List<Assignment>[] dueDateLists = {oneDayAssignments, threeDayAssignments, sevenDayAssignments};
            string[] dateLists = {"tomorrow", "in three days", "in one week"};
            for(int i = 0; i < dueDateLists.Length; i++)
                foreach (var assignment in dueDateLists[i])
                {
                    Course associatedCourse = DatabaseService.CourseCollection.FindOne(x =>
                        x.GuildId == assignment.GuildId && x.CourseCode == assignment.CourseCode);
                    
                    var guild = client.GetGuild(862894859060510741);
                    var role = guild.GetRole(associatedCourse.CourseRole);
                    
                    
                    
                    EmbedBuilder builder = new();

                    builder.WithTitle($"{assignment.Name} is due {dateLists[i]}!" );
                    builder.WithDescription("Make sure you get it done to stay safe from failing!");
                    builder.AddField(associatedCourse.CourseCode, "Course", false);
                    builder.AddField(assignment.DueDate.ToShortDateString(), "Due Daate", false);
                    builder.WithColor(Color.Blue);
                    
                    await guild.TextChannels.First().SendMessageAsync(role.Mention, false, builder.Build());
                }

        }
        
    }
}