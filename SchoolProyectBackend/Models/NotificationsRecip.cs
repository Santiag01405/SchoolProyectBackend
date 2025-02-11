using System.ComponentModel.DataAnnotations;

namespace SchoolProyectBackend.Models
{
    public class NotificationRecip
    {
        [Key]  // 🔹 Define la clave primaria
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public int RecipientId { get; set; }

        // Relaciones con otras entidades
        public Notification? Notification { get; set; }
    }
}
