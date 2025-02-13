using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ 1. Obtener todos los cursos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return await _context.Courses
                .Include(c => c.Teacher) // Incluye el profesor asignado al curso
                .ToListAsync();
        }

        // ✅ 2. Obtener un curso por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null) return NotFound();
            return course;
        }

        // ✅ 3. Crear un nuevo curso
        [HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(Course course)
        {
            // Verifica si el profesor asignado existe en la base de datos
            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherID == course.TeacherID);
            if (!teacherExists)
            {
                return BadRequest("El profesor especificado no existe.");
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseID }, course);
        }

        // ✅ 4. Actualizar un curso existente
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, Course updatedCourse)
        {
            if (id != updatedCourse.CourseID)
            {
                return BadRequest("El ID del curso no coincide.");
            }

            // Verifica si el profesor asignado existe en la base de datos
            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherID == updatedCourse.TeacherID);
            if (!teacherExists)
            {
                return BadRequest("El profesor especificado no existe.");
            }

            _context.Entry(updatedCourse).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ 5. Eliminar un curso
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
