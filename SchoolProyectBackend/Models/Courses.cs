using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Course
    {
        public int CourseID { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int TeacherID { get; set; }

        public Teacher? Teacher { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Grade>? Grades { get; set; }
    }
}
