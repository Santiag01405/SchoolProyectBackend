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

        // Eliminar salón
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null)
                return NotFound("Salón no encontrado.");

            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Salón eliminado correctamente." });
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
    }
}
