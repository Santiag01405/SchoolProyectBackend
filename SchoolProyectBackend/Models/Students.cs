using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Student
    {
        public int StudentID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Cedula { get; set; }
        public string? PhoneNumber { get; set; }
        public int? ParentID { get; set; }
        public int UserID { get; set; }

        public Parent? Parent { get; set; }
        public User? User { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
    }
}
