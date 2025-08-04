using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public School? School { get; set; }
        [JsonIgnore]
        public ICollection<User>? Users { get; set; }

    }

}
