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

        // Nuevo GET con schoolID integrado
        // GET: api/users?schoolId=5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] int schoolId)
        {
            try
            {
                Console.WriteLine($"🔍 Listando usuarios para SchoolID: {schoolId}");

                // 1) Todos los usuarios que sí pertenecen a la sede (alumnos, profesores, padres, admins, etc.)
                var usersInSchool = _context.Users
                    .Where(u => u.SchoolID == schoolId);

                // 2) IDs de estudiantes de la sede
                var studentIdsInSchool = _context.Users
                    .Where(u => u.RoleID == 1 && u.SchoolID == schoolId)
                    .Select(u => u.UserID);

                // 3) Padres (de cualquier sede) relacionados a esos estudiantes
                var parentIdsLinkedToSchool = _context.UserRelationships
                    .Where(ur => ur.RelationshipType == "Padre-Hijo"
                                 && studentIdsInSchool.Contains(ur.User1ID))
                    .Select(ur => ur.User2ID)
                    .Distinct();

                var parentsLinked = _context.Users
                    .Where(u => u.RoleID == 3 && parentIdsLinkedToSchool.Contains(u.UserID));

                // 4) Unión, evitando duplicados
                //    (Union ya evita duplicados si EF puede comparar por igualdad. Para asegurar,
                //    puedes agrupar por UserID después del ToList).
                var unionQuery = usersInSchool.Union(parentsLinked);

                var list = await unionQuery
                    .OrderBy(u => u.RoleID)         // opcional: orden por rol
                    .ThenBy(u => u.UserName)        // y por nombre
                    .ToListAsync();

                // Garantiza unicidad por UserID (por si acaso)
                var result = list
                    .GroupBy(u => u.UserID)
                    .Select(g => g.First())
                    .ToList();

                Console.WriteLine($"✅ Total devueltos (incl. padres vinculados de otras sedes): {result.Count}");

                return Ok(result);
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

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest(new { message = "El usuario no puede ser nulo" });

            // ✨✨✨ NUEVA VALIDACIÓN ✨✨✨
            // El número de teléfono debe ser requerido en la creación
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                return BadRequest(new { message = "El número de teléfono es requerido." });
            }


            // ✅ Validación: Verificar si ya existe un usuario con la misma cédula
            // y en el mismo colegio.
            var cedulaExists = await _context.Users
                .AnyAsync(u => u.Cedula == user.Cedula && u.SchoolID == user.SchoolID);

            if (cedulaExists)
            {
                return Conflict(new { message = "Ya existe un usuario con esta cédula en el mismo colegio." });
            }

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
                // ... (Tu código actual para manejar errores de la base de datos se mantiene)
                return StatusCode(500, new { message = "Error en la base de datos", error = ex.Message });
            }
            catch (Exception ex)
            {
                // ... (Tu código actual para manejar errores inesperados se mantiene)
                return StatusCode(500, new { message = "Error inesperado", error = ex.Message });
            }
        }

        [HttpGet("by-cedula/{cedula}")]
        public async Task<IActionResult> GetUserByCedula(string cedula, [FromQuery] int schoolId)
        {
            try
            {
                Console.WriteLine($"🔍 Buscando usuario con cédula: {cedula} para el SchoolID: {schoolId}");

                // 1) Buscar usuario en la misma sede
                var userInSchool = await _context.Users
                    .Include(u => u.School)
                    .FirstOrDefaultAsync(u => u.Cedula == cedula && u.SchoolID == schoolId);

                if (userInSchool != null)
                {
                    Console.WriteLine($"✅ Usuario encontrado en la sede: {userInSchool.UserName}, con cédula: {userInSchool.Cedula}");
                    return Ok(userInSchool);
                }

                // 2) Si no está en la sede, revisar si es un padre relacionado a un estudiante de esta sede
                var studentIdsInSchool = await _context.Users
                    .Where(u => u.RoleID == 1 && u.SchoolID == schoolId)
                    .Select(u => u.UserID)
                    .ToListAsync();

                if (!studentIdsInSchool.Any())
                {
                    return NotFound(new { message = "No hay estudiantes en esta sede para validar relaciones." });
                }

                var parentIdsLinked = await _context.UserRelationships
                    .Where(ur => ur.RelationshipType == "Padre-Hijo" &&
                                 studentIdsInSchool.Contains(ur.User1ID))
                    .Select(ur => ur.User2ID)
                    .Distinct()
                    .ToListAsync();

                var parentUser = await _context.Users
                    .Include(u => u.School)
                    .FirstOrDefaultAsync(u => parentIdsLinked.Contains(u.UserID) && u.Cedula == cedula);

                if (parentUser != null)
                {
                    Console.WriteLine($"✅ Padre encontrado en otra sede: {parentUser.UserName}, con cédula: {parentUser.Cedula}");
                    return Ok(parentUser);
                }

                Console.WriteLine("⚠ Usuario no encontrado.");
                return NotFound(new { message = "Usuario no encontrado en la sede ni como padre relacionado." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en GetUserByCedula: {ex.Message}");
                return StatusCode(500, $"Error interno: {ex.Message}");
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

        /*[HttpPut("{id}")]
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
        }*/

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User userUpdate)
        {
            if (id != userUpdate.UserID)
                return BadRequest();

            var schoolExists = await _context.Schools.AnyAsync(s => s.SchoolID == userUpdate.SchoolID);
            if (!schoolExists)
                return BadRequest(new { message = "El SchoolID no es válido." });

            // 1. En lugar de modificar la entidad completa, carga el usuario existente.
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);
            if (existingUser == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // 2. Crea una nueva instancia de la entidad User con la información actualizada y el ClassroomID original.
            var userToUpdate = new User
            {
                UserID = existingUser.UserID,
                UserName = userUpdate.UserName ?? existingUser.UserName,
                Email = userUpdate.Email ?? existingUser.Email,
                PasswordHash = userUpdate.PasswordHash ?? existingUser.PasswordHash,
                RoleID = userUpdate.RoleID,
                SchoolID = userUpdate.SchoolID,
                // Conserva el ClassroomID existente si no se proporciona uno nuevo
                ClassroomID = userUpdate.ClassroomID ?? existingUser.ClassroomID,
                Cedula = userUpdate.Cedula ?? existingUser.Cedula,
                PhoneNumber = userUpdate.PhoneNumber ?? existingUser.PhoneNumber
            };

            // 3. Marca la nueva entidad como modificada para que Entity Framework la actualice.
            _context.Entry(userToUpdate).State = EntityState.Modified;

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
        public async Task<ActionResult<IEnumerable<User>>> SearchUsers(
    [FromQuery] string query,
    [FromQuery] int schoolId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Debe proporcionar un término de búsqueda.");

            query = query.Trim();

            // IDs de estudiantes en la sede
            var studentIdsInSchool = _context.Users
                .Where(u => u.RoleID == 1 && u.SchoolID == schoolId)
                .Select(u => u.UserID);

            // Padres relacionados a esos estudiantes (sin importar SchoolID del padre)
            var parentIdsLinkedToSchool = _context.UserRelationships
                .Where(ur => ur.RelationshipType == "Padre-Hijo"
                             && studentIdsInSchool.Contains(ur.User1ID))
                .Select(ur => ur.User2ID)
                .Distinct();

            // Alumnos de la sede que matcheen por nombre o cédula
            var studentsQuery = _context.Users
                .Where(u => u.RoleID == 1 && u.SchoolID == schoolId &&
                            (EF.Functions.Like(u.UserName, $"%{query}%") ||
                             EF.Functions.Like(u.Cedula, $"%{query}%")));

            // Padres (de cualquier sede) que matcheen y estén relacionados a alumnos de esta sede
            var parentsQuery = _context.Users
                .Where(u => u.RoleID == 3 &&
                            parentIdsLinkedToSchool.Contains(u.UserID) &&
                            (EF.Functions.Like(u.UserName, $"%{query}%") ||
                             EF.Functions.Like(u.Cedula, $"%{query}%")));

            // Unión y tope de resultados
            var result = await studentsQuery
                .Union(parentsQuery)
                .OrderBy(u => u.UserName)
                .Take(50)
                .ToListAsync();

            if (!result.Any())
                return NotFound("No se encontraron usuarios con el criterio indicado.");

            return Ok(result);
        }

        [HttpGet("active-count-by-school")]
        public ActionResult<int> GetActiveUsersCountBySchool([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.SchoolID == schoolId);
        }

        [HttpGet("active-count")]
        public ActionResult<int> GetActiveUsersCount([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.SchoolID == schoolId);
        }

        [HttpGet("active-count-students")]
        public ActionResult<int> GetActiveStudentsCount([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.RoleID == 1 && u.SchoolID == schoolId);
        }

        [HttpGet("active-count-teachers")]
        public ActionResult<int> GetActiveTeachersCount([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.RoleID == 2 && u.SchoolID == schoolId);
        }

        [HttpGet("active-count-parents")]
        public ActionResult<int> GetActiveParentsCount([FromQuery] int schoolId)
        {
            return _context.Users.Count(u => u.RoleID == 3 && u.SchoolID == schoolId);
        }

    }
}
