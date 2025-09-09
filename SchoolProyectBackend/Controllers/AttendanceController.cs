using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

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

        private DateTime GetVenezuelanTime()
        {
            var venezuelaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Venezuela Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, venezuelaTimeZone);
        }
        // POST: api/attendance/mark
        // Mantiene: notifica a padres sin filtrar por sede del padre.
        // Etiqueta cada notificación con la sede del alumno (attendance.SchoolID).
        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] List<Attendance> attendanceList)
        {
            if (attendanceList == null || attendanceList.Count == 0)
                return BadRequest("No hay datos de asistencia.");

            var venezuelanTime = GetVenezuelanTime();

            foreach (var attendance in attendanceList)
            {
                var user = await _context.Users.FindAsync(attendance.UserID);
                var course = await _context.Courses.FindAsync(attendance.CourseID);

                if (user == null || course == null || user.SchoolID != attendance.SchoolID || course.SchoolID != attendance.SchoolID)
                    return BadRequest("Usuario o curso no pertenecen al colegio indicado.");

                attendance.Date = venezuelanTime;
            }

            _context.Attendance.AddRange(attendanceList);
            await _context.SaveChangesAsync();

            var notifications = new List<Notification>();

            foreach (var attendance in attendanceList)
            {
                if (attendance.Status == "Ausente")
                {
                    var parents = await _context.UserRelationships
                        .Where(ur => ur.User1ID == attendance.UserID && ur.RelationshipType == "Padre-Hijo") // 👈 sin filtrar por sede del padre
                        .Select(ur => ur.User2ID)
                        .ToListAsync();

                    // (opcional) incluir nombre del estudiante para mejor UX
                    var studentName = await _context.Users
                        .Where(u => u.UserID == attendance.UserID)
                        .Select(u => u.UserName)
                        .FirstOrDefaultAsync();

                    foreach (var parentId in parents)
                    {
                        notifications.Add(new Notification
                        {
                            UserID = parentId,
                            Title = "Notificación de Asistencia",
                            Content = $"Tu hijo/a {studentName} ha sido marcado como {attendance.Status} en la clase.",
                            Date = venezuelanTime,
                            SchoolID = attendance.SchoolID // 👈 sede del alumno
                        });
                    }
                }
            }

            if (notifications.Count > 0)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.PostAsJsonAsync(
                        "https://SchoolProject123.somee.com//api/notifications/batch",
                        notifications
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error al enviar notificaciones: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Excepción al intentar enviar notificaciones: {ex.Message}");
                }
            }

            return Ok(new { message = "Asistencia registrada y notificaciones enviadas." });
        }

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

        // GET: api/attendance/parent/{userId}[?schoolId=5]
        // Si no envías schoolId (o es <= 0), devuelve asistencias de TODOS los hijos del padre en TODAS las sedes.
        // Si envías schoolId > 0, filtra por esa sede.
        [HttpGet("parent/{userId}")]
        public async Task<IActionResult> GetAttendanceByParent(int userId, [FromQuery] int? schoolId = null)
        {
            // Hijos del padre (de cualquier sede)
            var studentIds = await _context.UserRelationships
                .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User1ID)
                .ToListAsync();

            if (!studentIds.Any())
                return Ok(new List<object>());

            var q = _context.Attendance.AsQueryable()
                .Where(a => studentIds.Contains(a.UserID));

            if (schoolId.HasValue && schoolId.Value > 0)
                q = q.Where(a => a.SchoolID == schoolId.Value);

            var attendanceRecords = await q
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
                    a.Status,
                    a.SchoolID // 👈 importante: la sede de la asistencia
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return Ok(attendanceRecords);
        }


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