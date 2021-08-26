using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Failsafe
{
     [Group("fs")]
    public class Commands : ModuleBase<SocketCommandContext>
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
                
                if (!isUserFailsafeManager(Context))
                {
                    Context.Message.ReplyAsync("You don't have permission to do that!");
                    return;
                }
                
                courseCode = courseCode.ToUpper();
                Course? assignmentCourse = DatabaseService.CourseCollection.Find(x => x.CourseCode == courseCode && x.GuildId == Context.Guild.Id).FirstOrDefault();
                
                if (assignmentCourse == null)
                {  
                    await Context.Message.ReplyAsync($"Invalid Course!");
                    return;
                }

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

                DatabaseService.AssignmentCollection.Insert(new Assignment()
                {
                    CourseCode = courseCode,
                    DueDate = dueDate,
                    Name = name,
                    GuildId = Context.Guild.Id
                });
                
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
        public async Task UserInfoAsync(string courseCode, SocketRole courseRole)
        {
            try
            {
                if (!isUserFailsafeManager(Context))
                {
                    Context.Message.ReplyAsync("You don't have permission to do that!");
                    return;
                }
                    
                courseCode = courseCode.ToUpper();

                Course newCourse = new Course()
                {
                    CourseCode = courseCode,
                    CourseRole = courseRole.Id,
                    GuildId = Context.Guild.Id
                };

                DatabaseService.CourseCollection.Insert(newCourse);
                
                EmbedBuilder builder = new();

                builder.WithTitle($"Created Course {courseCode}");
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
        
        [Command("findcourses")]
        [Summary("Gets a list of courses")]
        public async Task FindCoursesAsync()
        {
            try
            {
                List<Course> courses = DatabaseService.CourseCollection.FindAll().ToList();
                
                EmbedBuilder builder = new();

                builder.WithTitle($"Courses for {Context.Guild.Name}");
                builder.WithDescription("Course listing.");

                foreach (Course course in courses)
                {
                    builder.AddField($"{course.CourseCode}", "Course Code", false);
                }
                
                builder.WithColor(builder.Fields.Count == 0 ? Color.Red : Color.Blue);
                
                if(builder.Fields.Count == 0)
                    builder.AddField($"No Courses Found", "See '/fs help' for instructions on how to add more", false); 
                
                await Context.Message.ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }
        
        [Command("upcoming")]
        [Summary("Gets a list of courses")]
        public async Task FindUpcomingAssignmentsAsync()
        {
            try
            {
                List<Assignment> assignments = await GetUserAssignments(Context.User);

                EmbedBuilder builder = new();
                builder.WithTitle($"Upcoming Assignments for {((SocketGuildUser)Context.User).Nickname}");
                builder.WithDescription("Assignments due within the next 2 weeks.");

                foreach (Assignment assignment in assignments)
                {
                    
                    builder.AddField($"{assignment.Name}", "Assigment Name", false);
                    builder.AddField($"{assignment.DueDate.ToShortDateString()}", "Assigment Due Date", false);
                    builder.AddField($"{assignment.CourseCode}", "Course Code", false);
                    builder.AddField($"---------------", ".", false);
                }
                
                builder.WithColor(builder.Fields.Count == 0 ? Color.Red : Color.Blue);
                
                if(builder.Fields.Count == 0)
                    builder.AddField($"No Assignments Found", "See '/fs help' for instructions on how to add more", false);
                
                await Context.Message.ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }
        
        [Command("mycourses")]
        [Summary("Gets a list of courses")]
        public async Task FindMyCoursesAsync()
        {
            try
            {
                List<Course> courses = await GetUserCourses(Context.User);
            
                EmbedBuilder builder = new();

                builder.WithTitle($"Courses for {((SocketGuildUser)Context.Message.Author).Nickname}");
                builder.WithDescription("Courses that you are enrolled in.");
               

                foreach (Course course in courses)
                {
                    builder.AddField($"{course.CourseCode}", "Course Code", false);
                }
                
                builder.WithColor(builder.Fields.Count == 0 ? Color.Red : Color.Blue);
                
                if(builder.Fields.Count == 0)
                    builder.AddField($"No Courses Found", "See '/fs help' for instructions on how to add more", false);
                builder.WithFooter("Don't see one of your classes? Look in your server's role assignment channel.");
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
                await AlertService.SendAlerts(Context.Client);
                EmbedBuilder builder = new();

                builder.WithTitle("Help for Failsafe Bot");
                builder.WithDescription("Failsafe bot keeps you from failing your classes.");
                builder.AddField("/fs help", "shows this screen", false); // true - for inline
                builder.AddField("/fs add [date (mmddyyyy)] [course code] [assignment name]", "adds an assignment", false);
                builder.AddField("/fs addcourse [course code] [course role]", "adds a course", false);
                builder.AddField("/fs mycourses", "shows your current courses", false);
                builder.AddField("/fs upcoming", "shows your upcoming assignment deadlines", false);
                builder.AddField("/fs man", "displays manual page for server admins", false);

                builder.WithColor(Color.Blue);

                await Context.Message.ReplyAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }
        
        [Command("man")]
        [Summary("Returns usage information about this bot")]
        public async Task SendManpageAsync()
        {
            try
            {
                await AlertService.SendAlerts(Context.Client);
                EmbedBuilder builder = new();

              
                await Context.Message.ReplyAsync(
                    "Manual for Failsafe Bot\n"+
                    "1) Failsafe bot will attempt to send messages to #reminders, if it does not exist it will try #announcements and then #general\n\n"+
                    "2) Failsafe bot will only allow users with the role 'failsafe-manager' add classes and assignments");
            }
            catch (Exception ex)
            {
                await ErrorReporter.ReportErrorAsync(ex, Context);
            }
        }
        
        
        
        
        public async Task<List<Course>> GetUserCourses(SocketUser user)
        {
            List<Course> courses = new();

            foreach (var role in ((SocketGuildUser) user).Roles)
                courses.AddRange(DatabaseService.CourseCollection
                    .Find(x => x.CourseRole == role.Id && x.GuildId == Context.Guild.Id).ToList());

            return courses;
        }
        
        public async Task<List<Assignment>> GetUserAssignments(SocketUser user)
        {
            List<Course> courses = await GetUserCourses(user );
            List<Assignment> assignments = new();
            foreach (var course in courses)
                assignments.AddRange(DatabaseService.AssignmentCollection
                    .Find(x => x.CourseCode == course.CourseCode && x.GuildId == Context.Guild.Id && x.DueDate < DateTime.UtcNow.AddDays(14) && x.DueDate > DateTime.UtcNow.AddDays(-1)).ToList());

            return assignments.OrderByDescending(x => x.DueDate).ToList();
        }

        public bool isUserFailsafeManager(SocketCommandContext context)
        {
            SocketGuildUser user = (SocketGuildUser) context.User;

            return user.Roles.Count(x => x.Name == "failsafe-manager") > 0;
        }
    }
}