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

        [HttpPost("mark")]
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
        }

        /*[HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] List<Attendance> attendanceList)
        {
            if (attendanceList == null || attendanceList.Count == 0)
                return BadRequest("No hay datos de asistencia.");

            _context.Attendance.AddRange(attendanceList);
            await _context.SaveChangesAsync();

            // Notificar a los padres si el estudiante está ausente
            foreach (var attendance in attendanceList)
            {
                if (attendance.Status == "Ausente")
                {
                    var parents = await _context.UserRelationships
                        .Where(ur => ur.User1ID == attendance.UserID && ur.RelationshipType == "Padre-Hijo")
                        .Select(ur => ur.User2ID)
                        .ToListAsync();

                    foreach (var parent in parents)
                    {
                        var notification = new Notification
                        {
                            UserID = parent,
                            Title = "Notificacion de asistencia" ,
                            Content = $"Tu hijo ha sido marcado como {attendance.Status} en la clase."
                        };
                        _context.Notifications.Add(notification);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Asistencia registrada y notificación enviada." });
        }*/

        [HttpGet("course/{courseId}")]
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
        }

        [HttpGet("parent/{userId}")]
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
        }


    }
}