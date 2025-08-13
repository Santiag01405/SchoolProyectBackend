// DTOs/ExtracurricularEnrollmentDto.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolProyectBackend.DTOs
{
    public class ExtracurricularEnrollmentDto
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        public int ActivityID { get; set; }
        [Required]
        public int SchoolID { get; set; }
    }
}