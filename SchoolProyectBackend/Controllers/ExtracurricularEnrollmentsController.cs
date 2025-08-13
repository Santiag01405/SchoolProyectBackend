// Controllers/ExtracurricularEnrollmentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.DTOs;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/extracurriculars/enrollments")]
[ApiController]
public class ExtracurricularEnrollmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ExtracurricularEnrollmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> EnrollStudentInActivity([FromBody] ExtracurricularEnrollmentDto enrollmentDto)
    {
        // 1. Validar que el estudiante, la actividad y la escuela existan y coincidan
        var student = await _context.Users.FirstOrDefaultAsync(u => u.UserID == enrollmentDto.UserID && u.SchoolID == enrollmentDto.SchoolID);
        if (student == null)
            return BadRequest("El estudiante no existe en esta escuela.");

        var activity = await _context.ExtracurricularActivities.FirstOrDefaultAsync(a => a.ActivityID == enrollmentDto.ActivityID && a.SchoolID == enrollmentDto.SchoolID);
        if (activity == null)
            return BadRequest("La actividad no existe en esta escuela.");

        // 2. Verificar si la inscripción ya existe
        var existingEnrollment = await _context.ExtracurricularEnrollments
            .AnyAsync(e => e.UserID == enrollmentDto.UserID && e.ActivityID == enrollmentDto.ActivityID && e.SchoolID == enrollmentDto.SchoolID);
        if (existingEnrollment)
            return Conflict("El estudiante ya está inscrito en esta actividad.");

        // 3. Crear y guardar la nueva inscripción
        var newEnrollment = new ExtracurricularEnrollment
        {
            UserID = enrollmentDto.UserID,
            ActivityID = enrollmentDto.ActivityID,
            SchoolID = enrollmentDto.SchoolID
        };

        _context.ExtracurricularEnrollments.Add(newEnrollment);
        await _context.SaveChangesAsync();

        return Created("", newEnrollment);
    }

    [HttpGet("activity/{activityId}/students")]
    public async Task<ActionResult<IEnumerable<object>>> GetStudentsByActivity(int activityId, [FromQuery] int schoolId)
    {
        var students = await _context.ExtracurricularEnrollments
            .Where(e => e.ActivityID == activityId && e.SchoolID == schoolId)
            .Include(e => e.User)
            .Select(e => new
            {
                e.UserID,
                StudentName = e.User.UserName
            })
            .ToListAsync();

        if (!students.Any())
            return NotFound("No hay estudiantes inscritos en esta actividad.");

        return Ok(students);
    }

    [HttpGet("student/{userId}")]
    public async Task<ActionResult<IEnumerable<ExtracurricularActivity>>> GetActivitiesByStudent(int userId)
    {
        var activities = await _context.ExtracurricularEnrollments
            .Where(e => e.UserID == userId)
            .Include(e => e.ExtracurricularActivity)
            .Select(e => e.ExtracurricularActivity)
            .ToListAsync();

        if (!activities.Any())
            return NotFound("No hay actividades inscritas para este estudiante.");

        return Ok(activities);
    }
}