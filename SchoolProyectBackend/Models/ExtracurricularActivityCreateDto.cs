// DTOs/ExtracurricularActivityCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolProyectBackend.DTOs
{
    public class ExtracurricularActivityCreateDto
    {
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int? UserID { get; set; }
        public int? DayOfWeek { get; set; }
        [Required]
        public int SchoolID { get; set; }
    }
}