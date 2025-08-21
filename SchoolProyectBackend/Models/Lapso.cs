// Models/Lapso.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolProyectBackend.Models;

[Table("Lapsos")]
public class Lapso
{
    [Key]
    public int LapsoID { get; set; }

    [Required]
    public string Nombre { get; set; } // Ej: "Primer Lapso", "Segundo Lapso"

    [Required]
    public DateTime FechaInicio { get; set; }

    [Required]
    public DateTime FechaFin { get; set; }

    [Required]
    public int SchoolID { get; set; } // Vincula el lapso a una escuela

    [ForeignKey("SchoolID")]
    public School? School { get; set; } // Propiedad de navegación
}