using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SchoolProyectBackend.Models;
using System.Text.Json.Serialization;

namespace SchoolProyectBackend.Models
{
    public class Teacher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int TeacherID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int? UserID { get; set; }

        public User? User { get; set; }

        [JsonIgnore]
        public ICollection<Course>? Courses { get; set; }
    }
}
