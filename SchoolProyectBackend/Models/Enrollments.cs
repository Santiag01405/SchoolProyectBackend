using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Models
{
    public class Enrollment
    {
       
            public int EnrollmentID { get; set; }

            public int UserID { get; set; } 
            public User? User { get; set; } 

            public int CourseID { get; set; } 
            public Course? Course { get; set; }
            public int SchoolID { get; set; }
            public School? School { get; set; }

    }

}

