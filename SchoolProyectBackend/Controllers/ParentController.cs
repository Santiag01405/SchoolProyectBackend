using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/parents")]
    [ApiController]
    public class ParentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/parents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Parent>>> GetParents()
        {
            return await _context.Parents.Include(p => p.User).ToListAsync();
        }

        // ✅ GET: api/parents/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetParentById(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(p => p.ParentID == id);

            if (parent == null)
            {
                return NotFound();
            }

            return new
            {
                parent.ParentID,
                parent.FirstName,
                parent.LastName,
                parent.UserID,
                Role = parent.User?.Role?.RoleName ?? "Unknown"
            };
        }

        // ✅ POST: api/parents (Crear Parent)
        [HttpPost]
        public async Task<ActionResult<Parent>> CreateParent(Parent parent)
        {
            // Verificar si el usuario existe
            var userExists = await _context.Users.AnyAsync(u => u.UserID == parent.UserID);
            if (!userExists)
            {
                return BadRequest("El usuario especificado no existe.");
            }

            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParentById), new { id = parent.ParentID }, parent);
        }

        // ✅ PUT: api/parents/{id} (Actualizar Parent)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateParent(int id, Parent parentUpdate)
        {
            if (id != parentUpdate.ParentID)
            {
                return BadRequest();
            }

            _context.Entry(parentUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ DELETE: api/parents/{id} (Eliminar Parent)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParent(int id)
        {
            var parent = await _context.Parents.FindAsync(id);
            if (parent == null)
            {
                return NotFound();
            }

            _context.Parents.Remove(parent);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
