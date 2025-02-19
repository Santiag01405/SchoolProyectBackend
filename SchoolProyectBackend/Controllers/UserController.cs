    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SchoolProyectBackend.Data;
    using SchoolProyectBackend.Models;

    namespace SchoolProyectBackend.Controllers
    {
        [Route("api/users")]
        [ApiController]
        public class UserController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public UserController(ApplicationDbContext context)
            {
                _context = context;
            }

            // GET: api/users 
            [HttpGet]
            public async Task<ActionResult<IEnumerable<User>>> GetUsers()
            {
                return await _context.Users.ToListAsync();
            }

            // GET: api/users/{id} (Obtener un usuario por ID)
            [HttpGet("{id}")]
            public async Task<ActionResult<User>> GetUserById(int id)
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound();
                }

                return user;
            }

        // ✅ POST: api/users (Crear usuario)
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest(new { message = "El usuario no puede ser nulo" });
            }

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserById), new { id = user.UserID }, user);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error en la base de datos", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error inesperado", error = ex.Message });
            }
        }

        // POST: api/users (Crear usuario)
        /*[HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = user.UserID }, user);
        }
        */

        // PUT: api/users/{id} (Actualizar usuario)
        [HttpPut("{id}")]
            public async Task<IActionResult> UpdateUser(int id, User userUpdate)
            {
                if (id != userUpdate.UserID)
                {
                    return BadRequest();
                }

                _context.Entry(userUpdate).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.UserID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }


        // DELETE: api/users/{id} (Eliminar usuario)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }
    }
}
