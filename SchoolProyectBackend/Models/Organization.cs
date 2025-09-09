using System;
using System.Collections.Generic;

namespace SchoolProyectBackend.Models
{
    public class Organization
    {
        public int OrganizationID { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<School> Schools { get; set; } = new List<School>();
    }
}
