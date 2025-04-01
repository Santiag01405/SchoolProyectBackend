using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SchoolProyectBackend.Models;

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

    
    [JsonIgnore]
    public Course? Course { get; set; }

    [JsonIgnore]
    public User? User { get; set; }
}

