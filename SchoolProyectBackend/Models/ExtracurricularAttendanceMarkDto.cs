// DTOs/ExtracurricularAttendanceMarkDto.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SchoolProyectBackend.DTOs
{
    public class ExtracurricularAttendanceMarkDto
    {
        [Required]
        public int ActivityID { get; set; }
        [Required]
        public int RelatedUserID { get; set; }
        [Required]
        public int SchoolID { get; set; }
        [Required]
        public List<StudentAttendanceDto> StudentAttendance { get; set; } = new List<StudentAttendanceDto>();
    }

    public class StudentAttendanceDto
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        public string Status { get; set; }
    }
}