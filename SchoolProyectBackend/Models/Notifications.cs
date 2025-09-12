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

        [Column("isRead")]
        public bool IsRead { get; set; } = false;

        [Column("readDate")]
        public DateTime? ReadDate { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User? User { get; set; }

        public int SchoolID { get; set; }
        public School? School { get; set; }
    }
}
