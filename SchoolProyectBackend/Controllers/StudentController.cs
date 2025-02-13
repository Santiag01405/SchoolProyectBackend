using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/students")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/students (Obtener todos los estudiantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetStudents()
        {
            var students = await _context.Students
                .Include(s => s.User)
                .ThenInclude(u => u.Role) // Incluir el rol del usuario
                .Include(s => s.Parent)   // Incluir el Parent si existe
                .ToListAsync();

            return students.Select(s => new
            {
                s.StudentID,
                s.FirstName,
                s.LastName,
                s.UserID,
                Role = s.User?.Role?.RoleName ?? "Unknown",
                ParentName = s.Parent != null ? $"{s.Parent.FirstName} {s.Parent.LastName}" : "No Parent"
            }).ToList();
        }

        // GET: api/students/{id} (Obtener un estudiante por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetStudentById(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .ThenInclude(u => u.Role)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            return new
            {
                student.StudentID,
                student.FirstName,
                student.LastName,
                student.UserID,
                Role = student.User?.Role?.RoleName ?? "Unknown",
                ParentName = student.Parent != null ? $"{student.Parent.FirstName} {student.Parent.LastName}" : "No Parent"
            };
        }

        // POST: api/students (Crear un nuevo estudiante)
        [HttpPost]
        public async Task<ActionResult<Student>> CreateStudent(Student student)
        {
            // Verificar si el usuario existe
            var userExists = await _context.Users.AnyAsync(u => u.UserID == student.UserID);
            if (!userExists)
            {
                return BadRequest("El usuario especificado no existe.");
            }

            // Verificar si el Parent existe (si tiene uno asignado)
            if (student.ParentID.HasValue)
            {
                var parentExists = await _context.Parents.AnyAsync(p => p.ParentID == student.ParentID);
                if (!parentExists)
                {
                    return BadRequest("El Parent especificado no existe.");
                }
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudentById), new { id = student.StudentID }, student);
        }

        // PUT: api/students/{id} (Actualizar un estudiante)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, Student studentUpdate)
        {
            if (id != studentUpdate.StudentID)
            {
                return BadRequest();
            }

            _context.Entry(studentUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/students/{id} (Eliminar un estudiante)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
