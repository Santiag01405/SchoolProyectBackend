using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class Notification
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 

        [Column("notificationID")]
        public int NotifyID { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User? User { get; set; }
    }
}
