using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SchoolProyectBackend.Models
{
    public class School
    {
        public int SchoolID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relaciones
        [JsonIgnore]
        public ICollection<User> Users { get; set; }
        public ICollection<Course> Courses { get; set; }
        public ICollection<Evaluation> Evaluations { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<UserRelationship> UserRelationships { get; set; }
        public ICollection<Attendance> Attendance { get; set; }
       // public ICollection<Grade> Grades { get; set; }
    }
}
