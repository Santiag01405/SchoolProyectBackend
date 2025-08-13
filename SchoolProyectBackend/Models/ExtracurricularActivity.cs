// Models/ExtracurricularActivity.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolProyectBackend.Models;
using System.Collections.Generic;

public class ExtracurricularActivity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ActivityID { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? UserID { get; set; }
    public User? User { get; set; }

    public int? DayOfWeek { get; set; }

    public int SchoolID { get; set; }
    public School? School { get; set; }

    public ICollection<ExtracurricularEnrollment> ExtracurricularEnrollments { get; set; } = new List<ExtracurricularEnrollment>();
}