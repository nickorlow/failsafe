using LiteDB;

namespace Failsafe
{
    public class Course
    {
        public string CourseCode { get; set; }
        public ulong CourseRole { get; set; }
        public ulong GuildId { get; set; }
        
        public ObjectId Id { get; set; }
    }
}