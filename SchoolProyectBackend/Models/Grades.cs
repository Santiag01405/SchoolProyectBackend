/*using SchoolProyectBackend.Models;

public class Grade
{
    public int GradeID { get; set; }
    public int UserID { get; set; }
    public int CourseID { get; set; }
    public int? EvaluationID { get; set; }
    public int SchoolID { get; set; }
    public decimal? GradeValue { get; set; }
    public string? Comments { get; set; }
    // --- CÓDIGO PARA AGREGAR ---
    // Estas son las propiedades que necesita el controlador para funcionar.
    public int StudentID { get; set; }
    public Student? Student { get; set; }
    // 🔹 Relaciones (Opcionales)
    public User? User { get; set; }
    public Course? Course { get; set; }
    public Evaluation? Evaluation { get; set; }
    public School? School { get; set; }
}*/


using SchoolProyectBackend.Models;

public class Grade
{
    public int GradeID { get; set; }
    public int UserID { get; set; }
    public int CourseID { get; set; }
    public int? EvaluationID { get; set; }
    public int SchoolID { get; set; }
    public decimal? GradeValue { get; set; }
    public string? Comments { get; set; }

    // 🔹 Relaciones (Opcionales)
    public User? User { get; set; }
    public Course? Course { get; set; }
    public Evaluation? Evaluation { get; set; }
    public School? School { get; set; }
}
