using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generar ID en BD
        public int StudentID { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? Cedula { get; set; }
        public string? PhoneNumber { get; set; }

        
        public int? ParentID { get; set; }
        public Parent? Parent { get; set; }

       
        [Required]
        public int UserID { get; set; }
        public User? User { get; set; }


        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Notification>? Notifications { get; set; }

        public ICollection<Grade>? Grades { get; set; }
    }
}
