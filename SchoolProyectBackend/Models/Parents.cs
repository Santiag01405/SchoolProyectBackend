using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SchoolProyectBackend.Models;
using System.Runtime.InteropServices;

namespace SchoolProyectBackend.Models
{
    public class Parent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  
        public int ParentID { get; set; }

        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }
        public ICollection<Student>? Students { get; set; }
    }
}
