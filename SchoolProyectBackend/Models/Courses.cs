using SchoolProyectBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Course
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CourseID { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? UserID { get; set; }
    public User? User { get; set; }

    public int? DayOfWeek { get; set; }

    public int SchoolID { get; set; }
    public School? School { get; set; }

    // 🔹 Nueva relación
    public int? ClassroomID { get; set; }
    public Classroom? Classroom { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Grade>? Grades { get; set; }
}
