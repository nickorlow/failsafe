namespace Failsafe
{
    public class Course
    {
        public string CourseCode { get; set; }
        public long CourseRole { get; set; }
        public long GuildId { get; set; }
        public string Id => CourseCode + GuildId;
    }
}