using SchoolProyectBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Evaluation
{
    [Key]
    public int EvaluationID { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int CourseID { get; set; }

    [Required]
    public int UserID { get; set; }

    public int SchoolID { get; set; }
    public School? School { get; set; }

    // 🔹 Nuevo campo para asignar el salón automáticamente
    public int? ClassroomID { get; set; }
    public Classroom? Classroom { get; set; }

    [JsonIgnore]
    public Course? Course { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    [Required]
    public int LapsoID { get; set; } // Clave foránea para vincular a un lapso

    [ForeignKey("LapsoID")]
    public Lapso? Lapso { get; set; } // Propiedad de navegación
}
