// Controllers/ExtracurricularAttendanceController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.DTOs;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/extracurriculars/attendance")]
[ApiController]
public class ExtracurricularAttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ExtracurricularAttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("mark")]
    public async Task<IActionResult> MarkAttendance([FromBody] ExtracurricularAttendanceMarkDto attendanceDto)
    {
        if (attendanceDto.StudentAttendance == null || !attendanceDto.StudentAttendance.Any())
            return BadRequest("No hay datos de asistencia para registrar.");

        var newAttendanceRecords = new List<ExtracurricularAttendance>();
        var notifications = new List<Notification>();

        foreach (var studentAttendance in attendanceDto.StudentAttendance)
        {
            // Validar que el estudiante, el profesor y la actividad existan y pertenezcan a la misma escuela
            var studentUser = await _context.Users.FindAsync(studentAttendance.UserID);
            var relatedUser = await _context.Users.FindAsync(attendanceDto.RelatedUserID);
            var activity = await _context.ExtracurricularActivities.FindAsync(attendanceDto.ActivityID);

            if (studentUser == null || relatedUser == null || activity == null ||
                studentUser.SchoolID != attendanceDto.SchoolID || relatedUser.SchoolID != attendanceDto.SchoolID || activity.SchoolID != attendanceDto.SchoolID)
                return BadRequest("Usuario, profesor o actividad no pertenecen al colegio indicado.");

            // Crear el registro de asistencia
            var attendanceRecord = new ExtracurricularAttendance
            {
                UserID = studentAttendance.UserID,
                RelatedUserID = attendanceDto.RelatedUserID,
                ActivityID = attendanceDto.ActivityID,
                Date = DateTime.UtcNow.Date,
                Status = studentAttendance.Status,
                SchoolID = attendanceDto.SchoolID
            };
            newAttendanceRecords.Add(attendanceRecord);

                var parents = await _context.UserRelationships
                    .Where(ur => ur.User1ID == studentAttendance.UserID && ur.RelationshipType == "Padre-Hijo" && ur.SchoolID == attendanceDto.SchoolID)
                    .Select(ur => ur.User2ID)
                    .ToListAsync();

                foreach (var parentUserID in parents)
                {
                    notifications.Add(new Notification
                    {
                        UserID = parentUserID,
                        Title = "Notificación de Asistencia - Actividad Extracurricular",
                        Content = $"Su hijo(a) {studentUser.UserName} ha sido marcado como {studentAttendance.Status} en la actividad '{activity.Name}'.",
                        Date = DateTime.UtcNow,
                        SchoolID = attendanceDto.SchoolID,
                        IsRead = false
                    });
                }
            
        }

        _context.ExtracurricularAttendance.AddRange(newAttendanceRecords);
        if (notifications.Any())
        {
            _context.Notifications.AddRange(notifications);
        }
        await _context.SaveChangesAsync();

        return Ok(new { message = "Asistencia registrada y notificaciones enviadas correctamente." });
    }
}