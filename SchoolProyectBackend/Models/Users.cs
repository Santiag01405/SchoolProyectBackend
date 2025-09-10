using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SchoolProyectBackend.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int UserID { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }

        public string Cedula { get; set; }

        public required string PhoneNumber { get; set; }

        public int SchoolID { get; set; }

        public School? School { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public int? ClassroomID { get; set; }

        [ForeignKey("ClassroomID")]
        public Classroom? Classroom { get; set; }

        public int RoleID { get; set; }

        public Role? Role { get; set; }

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

}

