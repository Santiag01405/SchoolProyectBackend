using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    [Table("UserRelationships")]
    public class UserRelationship
    {
        [Key]
        public int RelationID { get; set; }

        [Required]
        public int User1ID { get; set; } // Usuario relacionado (Ej: Estudiante)

        [Required]
        public int User2ID { get; set; } // Otro usuario relacionado (Ej: Profesor, Tutor, Padre)

        [Required]
        [StringLength(20)]
        public string RelationshipType { get; set; } // Tipo de relación ('Padre-Hijo', 'Profesor-Estudiante', etc.)
    }
}
