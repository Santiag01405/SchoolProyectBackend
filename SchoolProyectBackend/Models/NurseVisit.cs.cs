// Models/NurseVisit.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class NurseVisit
    {
        [Key]
        public int VisitID { get; set; }

        [Required]
        [ForeignKey("StudentUser")]
        public int StudentUserID { get; set; }
        public User StudentUser { get; set; }

        public DateTime VisitDate { get; set; }

        [Required]
        [MaxLength(255)]
        public string Reason { get; set; }

        [MaxLength(255)]
        public string Treatment { get; set; }

        [Required]
        [ForeignKey("NurseUser")]
        public int NurseUserID { get; set; }
        public User NurseUser { get; set; }

        [Required]
        [ForeignKey("School")]
        public int SchoolID { get; set; }
        public School School { get; set; }
    }
}