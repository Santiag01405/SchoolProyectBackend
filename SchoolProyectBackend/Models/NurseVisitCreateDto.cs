// DTOs/NurseVisitCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolProyectBackend.DTOs
{
    public class NurseVisitCreateDto
    {
        [Required]
        public int StudentUserID { get; set; }

        [Required]
        public string Reason { get; set; }

        public string Treatment { get; set; }
    }
}