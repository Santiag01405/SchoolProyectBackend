using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolProyectBackend.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        /* [HttpPost("mark")]
         public async Task<IActionResult> MarkAttendance([FromBody] List<Attendance> attendanceList)
         {
             if (attendanceList == null || attendanceList.Count == 0)
                 return BadRequest("No hay datos de asistencia.");

             _context.Attendance.AddRange(attendanceList);
             await _context.SaveChangesAsync();

             List<Notification> notifications = new List<Notification>();

             // 🔹 Notificar a los padres si el estudiante está ausente
             foreach (var attendance in attendanceList)
             {
                 if (attendance.Status == "Ausente")
                 {
                     var parents = await _context.UserRelationships
                         .Where(ur => ur.User1ID == attendance.UserID && ur.RelationshipType == "Padre-Hijo")
                         .Select(ur => ur.User2ID)
                         .ToListAsync();

                     foreach (var userID in parents)
                     {
                         notifications.Add(new Notification
                         {
                             UserID = userID, // 🔹 El padre recibe la notificación
                             Title = "Notificación de Asistencia",
                             Content = $"Tu hijo ha sido marcado como {attendance.Status} en la clase.",
                             Date = DateTime.UtcNow
                         });
                     }
                 }
             }

             // 🔹 Enviar notificaciones a `NotificationController` usando HttpClient
             if (notifications.Count > 0)
             {
                 using var httpClient = new HttpClient();
                 var response = await httpClient.PostAsJsonAsync("http://localhost:5015/api/notifications/batch", notifications);

                 if (!response.IsSuccessStatusCode)
                     return StatusCode((int)response.StatusCode, "Error al enviar notificaciones.");
             }

             return Ok(new { message = "Asistencia registrada y notificaciones enviadas." });
         }*/

        //Con schoolid
        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] List<Attendance> attendanceList)
        {
            if (attendanceList == null || attendanceList.Count == 0)
                return BadRequest("No hay datos de asistencia.");

            // Validar que el usuario y el curso pertenezcan a la misma escuela
            foreach (var attendance in attendanceList)
            {
                var user = await _context.Users.FindAsync(attendance.UserID);
                var course = await _context.Courses.FindAsync(attendance.CourseID);

                if (user == null || course == null || user.SchoolID != attendance.SchoolID || course.SchoolID != attendance.SchoolID)
                    return BadRequest("Usuario o curso no pertenecen al colegio indicado.");
            }

            _context.Attendance.AddRange(attendanceList);
            await _context.SaveChangesAsync();

            List<Notification> notifications = new List<Notification>();

            foreach (var attendance in attendanceList)
            {
                if (attendance.Status == "Ausente")
                {
                    var parents = await _context.UserRelationships
                        .Where(ur => ur.User1ID == attendance.UserID && ur.RelationshipType == "Padre-Hijo")
                        .Select(ur => ur.User2ID)
                        .ToListAsync();

                    foreach (var userID in parents)
                    {
                        notifications.Add(new Notification
                        {
                            UserID = userID,
                            Title = "Notificación de Asistencia",
                            Content = $"Tu hijo ha sido marcado como {attendance.Status} en la clase.",
                            Date = DateTime.UtcNow
                        });
                    }
                }
            }

            if (notifications.Count > 0)
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync("http://localhost:5015/api/notifications/batch", notifications);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error al enviar notificaciones.");
            }

            return Ok(new { message = "Asistencia registrada y notificaciones enviadas." });
        }



        /* [HttpGet("course/{courseId}")]
         public async Task<IActionResult> GetAttendanceByCourse(int courseId)
         {
             var attendanceRecords = await _context.Attendance
                 .Where(a => a.CourseID == courseId)
                 .Select(a => new
                 {
                     a.AttendanceID,
                     a.UserID,
                     StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                     a.RelatedUserID,
                     TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                     a.CourseID,
                     CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                     a.Date,
                     a.Status
                 })
                 .ToListAsync();

             return Ok(attendanceRecords);
         }*/

        //Con schoolid
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetAttendanceByCourse(int courseId, [FromQuery] int schoolId)
        {
            var attendanceRecords = await _context.Attendance
                .Where(a => a.CourseID == courseId && a.SchoolID == schoolId)
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserID,
                    StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                    a.RelatedUserID,
                    TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                    a.CourseID,
                    CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                    a.Date,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendanceRecords);
        }


        /* [HttpGet("parent/{userId}")]
         public async Task<IActionResult> GetAttendanceByParent(int userId)
         {
             var studentIds = await _context.UserRelationships
                 .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo")
                 .Select(ur => ur.User1ID)
                 .ToListAsync();

             var attendanceRecords = await _context.Attendance
                 .Where(a => studentIds.Contains(a.UserID))
                 .Select(a => new
                 {
                     a.AttendanceID,
                     a.UserID,
                     StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                     a.RelatedUserID,
                     TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                     a.CourseID,
                     CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                     a.Date,
                     a.Status
                 })
                 .ToListAsync();

             return Ok(attendanceRecords);
         }*/

        //Con schoolid
        [HttpGet("parent/{userId}")]
        public async Task<IActionResult> GetAttendanceByParent(int userId, [FromQuery] int schoolId)
        {
            var studentIds = await _context.UserRelationships
                .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User1ID)
                .ToListAsync();

            var attendanceRecords = await _context.Attendance
                .Where(a => studentIds.Contains(a.UserID) && a.SchoolID == schoolId)
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserID,
                    StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                    a.RelatedUserID,
                    TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                    a.CourseID,
                    CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                    a.Date,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendanceRecords);
        }



        /*[HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.Attendance.FindAsync(id);

            if (attendance == null)
                return NotFound(new { message = "Asistencia no encontrada." });

            _context.Attendance.Remove(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Asistencia eliminada correctamente." });
        }*/

        //Con schoolid
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id, [FromQuery] int schoolId)
        {
            var attendance = await _context.Attendance.FindAsync(id);

            if (attendance == null || attendance.SchoolID != schoolId)
                return NotFound(new { message = "Asistencia no encontrada para esta escuela." });

            _context.Attendance.Remove(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Asistencia eliminada correctamente." });
        }


    }
}