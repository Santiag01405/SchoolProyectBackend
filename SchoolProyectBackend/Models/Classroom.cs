using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProyectBackend.Models
{
    public class Classroom
    {
        public int ClassroomID { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public int SchoolID { get; set; }

   
        public School? School { get; set; }
        public ICollection<User>? Users { get; set; }
    }

}
