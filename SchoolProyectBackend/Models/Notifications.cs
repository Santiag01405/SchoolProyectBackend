using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class Notification
    {
        [Key] // 🔹 Define explícitamente NotifyID como clave primaria
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 🔹 Autogenera el ID en la BD
        public int NotifyID { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int? StudentID { get; set; }
        public int? TeacherID { get; set; }
        public int? ParentID { get; set; }

        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
        public Parent? Parent { get; set; }
    }
}
