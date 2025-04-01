using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/schedule")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtener el horario de un estudiante
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserSchedule(int userId)
        {
            if (userId <= 0) return BadRequest("ID de usuario no válido.");

            int today = (int)DateTime.UtcNow.DayOfWeek;

            var schedule = await _context.Courses
                .Where(c => _context.Enrollments
                              .Any(e => e.UserID == userId && e.CourseID == c.CourseID)
                        && c.DayOfWeek == today)
                .ToListAsync();

            if (!schedule.Any()) return NotFound("No hay clases para este usuario hoy.");

            return Ok(schedule);
        }

        // Crear un nuevo curso (asignado a un horario)
        [HttpPost("create")]
        public async Task<IActionResult> CreateSchedule([FromBody] Course newCourse)
        {
            if (newCourse == null)
                return BadRequest("Datos del curso inválidos.");

            if (newCourse.DayOfWeek < 0 || newCourse.DayOfWeek > 6)
                return BadRequest("Día de la semana inválido.");

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserSchedule), new { userId = newCourse.UserID /*TeacherID*/ }, newCourse);
        }

        // Editar un horario existente
        [HttpPut("edit/{courseId}")]
        public async Task<IActionResult> EditSchedule(int courseId, [FromBody] Course updatedCourse)
        {
            if (courseId <= 0)
                return BadRequest("ID del curso inválido.");

            var existingCourse = await _context.Courses.FindAsync(courseId);
            if (existingCourse == null)
                return NotFound("Curso no encontrado.");

            existingCourse.Name = updatedCourse.Name;
            existingCourse.Description = updatedCourse.Description;
            existingCourse.UserID/*TeacherID*/ = updatedCourse.UserID/*TeacherID*/;
            existingCourse.DayOfWeek = updatedCourse.DayOfWeek;

            _context.Entry(existingCourse).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Eliminar un horario
        [HttpDelete("delete/{courseId}")]
        public async Task<IActionResult> DeleteSchedule(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound("Curso no encontrado.");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
