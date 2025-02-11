using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Grade
    {
        public int GradeID { get; set; }
        public int StudentID { get; set; }
        public int CourseID { get; set; }
        public decimal? GradeValue { get; set; }
        public string? Comments { get; set; }

        public Student? Student { get; set; }
        public Course? Course { get; set; }
    }
}
