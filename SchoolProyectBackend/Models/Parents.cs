using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Parent
    {
        public int ParentID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public int UserID { get; set; }

        public User? User { get; set; }
        public ICollection<Student>? Students { get; set; }
    }
}
