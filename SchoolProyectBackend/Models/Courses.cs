using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseID { get; set; }

        [Required]
        public required string Name { get; set; }

        public string? Description { get; set; }

        // Ahora el TeacherID es simplemente el userID del profesor
        //[ForeignKey("User")]
        public int? UserID { get; set; }

        public User? User { get; set; } // Relación con el usuario que es profesor

        public int? DayOfWeek { get; set; }

        // Relación con las inscripciones (enrollment)
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Grade>? Grades { get; set; }
    }
}


/*using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Course
    {
        public int CourseID { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
      
        public int DayOfWeek { get; set; }
        public int UserID { get; set; }

        public User? User { get; set; }
       
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Grade>? Grades { get; set; }
    }
}*/
