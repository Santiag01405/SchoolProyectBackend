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
        /*[HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }*/

        //Nuevo GET con schoolID integrado
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] int schoolId)
        {
            try
            {
                Console.WriteLine($"🔍 Buscando usuarios para el SchoolID: {schoolId}");

                var users = await _context.Users
                    .Where(u => u.SchoolID == schoolId)
                    .ToListAsync();

                Console.WriteLine($"✅ Se encontraron {users.Count} usuarios");

                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en GetUsers: {ex.Message}");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        // GET: api/users/{id} (Obtener un usuario por ID)
        /* [HttpGet("{id}")]
             public async Task<ActionResult<User>> GetUserById(int id)
             {
                 var user = await _context.Users.FindAsync(id);

                 if (user == null)
                 {
                     return NotFound();
                 }

                 return user;
             }*/
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                Console.WriteLine($"📌 [DEBUG] Buscando usuario con ID: {id}");

                var user = await _context.Users
                    .Include(u => u.School)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user == null)
                {
                    Console.WriteLine("⚠ Usuario no encontrado.");
                    return NotFound(new { message = "Usuario no encontrado." });
                }

                Console.WriteLine($"✅ Usuario encontrado: {user.UserName}, RoleID: {user.RoleID}");

                // ✅ Solo cargar Classroom si es estudiante y tiene ClassroomID asignado
                if (user.RoleID == 1 && user.ClassroomID.HasValue)
                {
                    Console.WriteLine($"📌 [DEBUG] Cargando Classroom con ID: {user.ClassroomID}");

                    var classroom = await _context.Classrooms
                        .FirstOrDefaultAsync(c => c.ClassroomID == user.ClassroomID);

                    if (classroom != null)
                    {
                        Console.WriteLine($"✅ Classroom encontrado: {classroom.Name}");
                        user.Classroom = classroom;
                    }
                    else
                    {
                        Console.WriteLine("⚠ No se encontró el Classroom asignado al estudiante.");
                    }
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                    errorDetails += " | Inner Exception: " + ex.InnerException.Message;

                Console.WriteLine($"❌ Error interno en GetUserById: {errorDetails}");

                return StatusCode(500, new
                {
                    message = "Error interno en el servidor al obtener usuario.",
                    error = errorDetails,
                    stackTrace = ex.StackTrace
                });
            }
        }


        // ✅ POST: api/users (Crear usuario)
        /* [HttpPost]
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
         }*/

        //Nuevo POST con schoolIFD integrado
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest(new { message = "El usuario no puede ser nulo" });

            var schoolExists = await _context.Schools.AnyAsync(s => s.SchoolID == user.SchoolID);
            if (!schoolExists)
                return BadRequest(new { message = "El SchoolID no es válido." });

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
        /* [HttpPut("{id}")]
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
             }*/

        //Nuevo PUT con schoolID integrado

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User userUpdate)
        {
            if (id != userUpdate.UserID)
                return BadRequest();

            var schoolExists = await _context.Schools.AnyAsync(s => s.SchoolID == userUpdate.SchoolID);
            if (!schoolExists)
                return BadRequest(new { message = "El SchoolID no es válido." });

            _context.Entry(userUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();

                throw;
            }
        }


        //DELETE

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Eliminar relaciones (UserRelationships)
                var relaciones = await _context.UserRelationships
                    .Where(r => r.User1ID == id || r.User2ID == id)
                    .ToListAsync();
                if (relaciones.Any())
                {
                    _context.UserRelationships.RemoveRange(relaciones);
                }

                // Eliminar inscripciones (Enrollments)
                var enrollments = await _context.Enrollments
                    .Where(e => e.UserID == id)
                    .ToListAsync();
                if (enrollments.Any())
                {
                    _context.Enrollments.RemoveRange(enrollments);
                }

                // Eliminar notificaciones (Notifications)
                var notifications = await _context.Notifications
                    .Where(n => n.UserID == id)
                    .ToListAsync();
                if (notifications.Any())
                {
                    _context.Notifications.RemoveRange(notifications);
                }

                // Finalmente eliminar el usuario
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                    errorDetails += " | Inner Exception: " + ex.InnerException.Message;

                return StatusCode(500, $"Error interno al eliminar el usuario: {errorDetails}");
            }
        }



        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        //Busqueda de usuario segun su UserName
        /*[HttpGet("search")]
        public async Task<ActionResult<IEnumerable<User>>> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Debe proporcionar un término de búsqueda.");
            }

            var users = await _context.Users
                .Where(u => (u.RoleID == 1 || u.RoleID == 3) && u.UserName.Contains(query))
                .ToListAsync();

            if (!users.Any())
            {
                return NotFound("No se encontraron usuarios con ese nombre.");
            }

            return Ok(users);
        }*/

        //Nuevo search con SchoolID integrado
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<User>>> SearchUsers([FromQuery] string query, [FromQuery] int schoolId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Debe proporcionar un término de búsqueda.");

            var users = await _context.Users
                .Where(u =>
                    (u.RoleID == 1 || u.RoleID == 3) &&
                    u.UserName.Contains(query) &&
                    u.SchoolID == schoolId
                ).ToListAsync();

            if (!users.Any())
                return NotFound("No se encontraron usuarios con ese nombre.");

            return Ok(users);
        }

        [HttpGet("active-count-by-school")]
        public ActionResult<int> GetActiveUsersCountBySchool([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.SchoolID == schoolId);
        }

        [HttpGet("active-count")]
        public ActionResult<int> GetActiveUsersCount()
        {
            return _context.Users.Count();
        }

        [HttpGet("active-count-students")]
        public ActionResult<int> GetActiveStudentsCount()
        {
            return _context.Users.Count(u => u.RoleID == 1);
        }

        [HttpGet("active-count-teachers")]
        public ActionResult<int> GetActiveTeachersCount()
        {
            return _context.Users.Count(u => u.RoleID == 2);
        }
        [HttpGet("active-count-parents")]
        public ActionResult<int> GetActiveParentsCount()
        {
            return _context.Users.Count(u => u.RoleID == 3);
        }
    }
}
