using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Role
    {
        public int RoleID { get; set; }
        public required string RoleName { get; set; }

        public ICollection<User>? Users { get; set; }
    }
}
