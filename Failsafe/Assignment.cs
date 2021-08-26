using System;
using LiteDB;

namespace Failsafe
{
    public class Assignment
    {
        public ulong GuildId { get; set; }
        public string Name { get; set; }
        public DateTime DueDate { get; set; }
        public string CourseCode { get; set; }
        public ObjectId Id { get; set; }
    }
}