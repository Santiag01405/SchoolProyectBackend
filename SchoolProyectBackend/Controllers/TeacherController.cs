using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/teachers")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/teachers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Teacher>>> GetTeachers()
        {
            return await _context.Teachers.ToListAsync();
        }

        // GET: api/teachers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTeacherById(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)          // Cargar la relación con User
                .ThenInclude(u => u.Role)      // Cargar la relación con Role dentro de User
                .FirstOrDefaultAsync(t => t.TeacherID == id);

            if (teacher == null)
            {
                return NotFound();
            }

            return new
            {
                teacher.TeacherID,
                teacher.FirstName,
                teacher.LastName,
                teacher.UserID,
                Role = teacher.User != null ? teacher.User.Role?.RoleName : "Unknown"
            };
        }


        // POST: api/teachers
        [HttpPost]
        public async Task<ActionResult<Teacher>> CreateTeacher(Teacher teacher)
        {
            // Verificar si el usuario existe en la base de datos
            var userExists = await _context.Users.AnyAsync(u => u.UserID == teacher.UserID);
            if (!userExists)
            {
                return BadRequest("El usuario especificado no existe.");
            }

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeacherById), new { id = teacher.TeacherID }, teacher);
        }


        // PUT: api/teachers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, Teacher teacherUpdate)
        {
            if (id != teacherUpdate.TeacherID)
            {
                return BadRequest();
            }

            _context.Entry(teacherUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/teachers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
