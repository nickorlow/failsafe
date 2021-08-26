using LiteDB;

namespace Failsafe
{
    public static class DatabaseService
    {
        public static LiteDatabase Database;
        public static ILiteCollection<Course> CourseCollection => Database.GetCollection<Course>("courses");
        public static ILiteCollection<Assignment> AssignmentCollection => Database.GetCollection<Assignment>("assignments");

        public static void Initialize()
        {
            Database = new LiteDatabase("./failsafe_db.db");
            InitializeDatabase();
        }
        
        
        
        private static void InitializeDatabase()
        {
            var courseCollection =  Database.GetCollection<Course>("courses");
            courseCollection.EnsureIndex(x => x.CourseCode + x.GuildId);
            
            Database.GetCollection<Assignment>("assignments");
        }
    }
}