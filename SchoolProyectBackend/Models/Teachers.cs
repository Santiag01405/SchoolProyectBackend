using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int UserID { get; set; }

        public User? User { get; set; }
        public ICollection<Course>? Courses { get; set; }
    }
}
