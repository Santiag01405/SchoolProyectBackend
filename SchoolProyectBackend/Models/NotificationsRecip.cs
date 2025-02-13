using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class NotificationRecip
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Column("recipientID")] // 🔹 Clave primaria con el nombre correcto en la BD
        public int RecipientId { get; set; }

        [Column("notificationID")]
        public int NotificationId { get; set; }

        public Notification? Notification { get; set; }
    }
}