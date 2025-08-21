using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Threading.Tasks;

namespace SchoolProyectBackend.Controllers
{
    [ApiController]
    [Route("api/classrooms")]
    public class ClassroomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClassroomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtener salones de un colegio
        [HttpGet]
        public async Task<IActionResult> GetClassrooms([FromQuery] int schoolId)
        {
            var classrooms = await _context.Classrooms
                .Where(c => c.SchoolID == schoolId)
                .ToListAsync();

            return Ok(classrooms);
        }

        // Crear un nuevo salón asociado a un colegio
        [HttpPost]
        public async Task<IActionResult> CreateClassroom(Classroom classroom)
        {
            var school = await _context.Schools.FindAsync(classroom.SchoolID);
            if (school == null)
                return NotFound("El colegio especificado no existe.");

            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Salón creado correctamente." });
        }

        // Actualizar salón
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClassroom(int id, Classroom updatedClassroom)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null)
                return NotFound("Salón no encontrado.");

            classroom.Name = updatedClassroom.Name;
            classroom.Description = updatedClassroom.Description;
            classroom.SchoolID = updatedClassroom.SchoolID;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Salón actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null)
            {
                return NotFound("Salón no encontrado.");
            }

            // 1. Desvincular todos los estudiantes del salón
            var studentsInClassroom = await _context.Users
                .Where(u => u.ClassroomID == id)
                .ToListAsync();

            if (studentsInClassroom.Any())
            {
                foreach (var student in studentsInClassroom)
                {
                    student.ClassroomID = null;
                }
            }

            // 2. Desvincular todos los cursos del salón
            var coursesInClassroom = await _context.Courses
                .Where(c => c.ClassroomID == id)
                .ToListAsync();

            if (coursesInClassroom.Any())
            {
                foreach (var course in coursesInClassroom)
                {
                    course.ClassroomID = null;
                }
            }

            // 3. Eliminar todas las evaluaciones asociadas al salón
            var evaluationsInClassroom = await _context.Evaluations
                .Where(e => e.ClassroomID == id)
                .ToListAsync();

            if (evaluationsInClassroom.Any())
            {
                _context.Evaluations.RemoveRange(evaluationsInClassroom);
            }

            // 4. Guardar los cambios para desvincular los registros y eliminar evaluaciones
            await _context.SaveChangesAsync();

            // 5. Ahora es seguro eliminar el salón
            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Salón y sus registros relacionados eliminados correctamente." });
        }
        // Asignar estudiante a un salón dentro de un colegio
        [HttpPut("assign/{userId}")]
        public async Task<IActionResult> AssignClassroomToStudent(int userId, [FromQuery] int classroomId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.RoleID == 1);
            if (user == null)
                return NotFound("Usuario no encontrado o no es estudiante.");

            var classroom = await _context.Classrooms.FindAsync(classroomId);
            if (classroom == null)
                return NotFound("Salón no encontrado.");

            user.ClassroomID = classroomId;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"El estudiante {user.UserName} fue asignado al salón {classroom.Name}." });
        }

        // Obtener un salón por su ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassroom(int id, [FromQuery] int schoolId)
        {
            var classroom = await _context.Classrooms
                .FirstOrDefaultAsync(c => c.ClassroomID == id && c.SchoolID == schoolId);

            if (classroom == null)
            {
                return NotFound("Salón no encontrado.");
            }

            return Ok(classroom);
        }

        // Obtener estudiantes en un salón
        // Obtener estudiantes en un salón
        [HttpGet("{classroomId}/students")]
        public async Task<IActionResult> GetStudentsInClassroom(int classroomId, [FromQuery] int schoolId)
        {
            var classroom = await _context.Classrooms.FindAsync(classroomId);
            if (classroom == null || classroom.SchoolID != schoolId)
            {
                return NotFound("Salón no encontrado en esta escuela.");
            }

            var students = await _context.Enrollments
                .Where(e => e.SchoolID == schoolId) // Filtramos por escuela
                .Include(e => e.Course)
                .Where(e => e.Course.ClassroomID == classroomId) // Filtramos por el ID del salón del curso
                .Include(e => e.User)
                .Where(e => e.User.RoleID == 1) // Aseguramos que sea un estudiante (RoleID = 1)
                .Select(e => new
                {
                    e.User.UserID,
                    e.User.UserName,
                    e.User.Email
                })
                .Distinct() // Evita duplicados si un estudiante está en varios cursos del mismo salón
                .ToListAsync();

            if (!students.Any())
            {
                return NotFound("No se encontraron estudiantes en este salón.");
            }

            return Ok(students);
        }
    }
}
