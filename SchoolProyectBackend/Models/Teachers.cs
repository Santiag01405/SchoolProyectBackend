using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SchoolProyectBackend.Models;
using System.Text.Json.Serialization;

namespace SchoolProyectBackend.Models
{
    public class Teacher
    {
   
        public int TeacherID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    

       
    }
}
