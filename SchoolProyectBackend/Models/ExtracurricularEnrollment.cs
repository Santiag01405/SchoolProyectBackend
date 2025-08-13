// Models/ExtracurricularEnrollment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class ExtracurricularEnrollment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EnrollmentID { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public int ActivityID { get; set; }

        [ForeignKey("ActivityID")]
        public ExtracurricularActivity? ExtracurricularActivity { get; set; }

        public int SchoolID { get; set; }
        public School? School { get; set; }
    }
}