// Models/ExtracurricularAttendance.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    [Table("ExtracurricularAttendance")]
    public class ExtracurricularAttendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int RelatedUserID { get; set; }

        [Required]
        public int ActivityID { get; set; }

        [ForeignKey("ActivityID")]
        public ExtracurricularActivity? ExtracurricularActivity { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(10)]
        public string Status { get; set; }

        public int SchoolID { get; set; }
        public School? School { get; set; }
    }
}