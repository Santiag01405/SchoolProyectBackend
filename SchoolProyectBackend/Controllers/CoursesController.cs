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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCourses()
        {
            var courses = await _context.Courses
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    c.Description,
                    DayOfWeek = c.DayOfWeek ?? 0,
                    UserID = c.UserID ?? 0 // ✅ Si `UserID` es NULL, devolver 0
                })
                .ToListAsync();

            return Ok(courses);
        }


        /*[HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            Console.WriteLine("📌 Intentando obtener cursos...");

            var courses = await _context.Courses
                .Include(c => c.User) // 🔹 Relación con el profesor (UserID)
                .ToListAsync();

            if (courses == null || courses.Count == 0)
            {
                Console.WriteLine("❌ No hay cursos en la base de datos.");
                return NotFound("No hay cursos disponibles.");
            }

            Console.WriteLine($"✅ Se encontraron {courses.Count} cursos.");
            return Ok(courses);
        }*/


        /*
        // ✅ 2. Obtener un curso por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null) return NotFound();
            return course;
        }*/


        // ✅ 2. Obtener un curso por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCourse(int id)
        {
            var course = await _context.Courses
                .Where(c => c.CourseID == id)
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    Description = c.Description ?? "Sin descripción",  // ✅ Si es NULL, mostrar mensaje
                    DayOfWeek = c.DayOfWeek,  // ✅ Si `DayOfWeek` es NULL, devuelve 0
                    UserID = c.UserID.HasValue ? c.UserID.Value : 0    // ✅ Si `UserID` es NULL, devuelve 0
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound(new { message = "Curso no encontrado" });

            return Ok(course);
        }


        // ✅ Crear un curso con `UserID` en lugar de `TeacherID`
        [HttpPost("create")]
        public async Task<ActionResult<Course>> CreateCourse([FromBody] Course courseRequest)
        {
            if (courseRequest == null || courseRequest.UserID == 0)
                return BadRequest("El `UserID` del profesor es obligatorio.");

            // Verificar si el usuario es un profesor
            var isTeacher = await _context.Users.AnyAsync(u => u.UserID == courseRequest.UserID && u.RoleID == 2);
            if (!isTeacher)
                return BadRequest("El usuario especificado no es un profesor.");

            // Crear el curso
            var course = new Course
            {
                Name = courseRequest.Name,
                Description = courseRequest.Description,
                UserID = courseRequest.UserID, // Asignamos `UserID` en lugar de `TeacherID`
                DayOfWeek = courseRequest.DayOfWeek
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseID }, course);
        }


        // ✅ 3. Crear un nuevo curso
        /*[HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(Course course)
        {
            // Verifica si el profesor asignado existe en la base de datos
            var teacherExists = await _context.Users.AnyAsync(t => t.UserID == course.UserID);
            if (!teacherExists)
            {
                return BadRequest("El profesor especificado no existe.");
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseID }, course);
        }*/

        // ✅ Obtener los cursos en los que un usuario es profesor
        [HttpGet("user/{userId}/taught-courses")]
        public async Task<IActionResult> GetCoursesByTeacher(int userId)
        {
            var courses = await _context.Courses
                .Where(c => c.UserID == userId) // 🔹 Buscar por `UserID`, no `UserRelationships`
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    Description = c.Description ?? "Sin descripción",
                    DayOfWeek = c.DayOfWeek ?? 0
                })
                .ToListAsync();

            if (courses == null || courses.Count == 0)
                return NotFound("No se encontraron cursos para este usuario.");

            return Ok(courses);
        }

        /* [HttpGet("user/{userId}/taught-courses")]
         public async Task<IActionResult> GetCoursesByTeacher(int userId)
         {
             var courses = await _context.Courses
                 .Where(c => _context.UserRelationships
                     .Any(ur => ur.User1ID == userId && ur.RelationshipType == "Profesor-Estudiante"))
                 .Select(c => new
                 {
                     c.CourseID,
                     c.Name,
                     c.Description,
                     c.DayOfWeek
                 })
                 .ToListAsync();

             if (courses == null || courses.Count == 0)
                 return NotFound("No se encontraron cursos para este usuario.");

             return Ok(courses);
         }*/


        // ✅ 4. Actualizar un curso existente
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, Course updatedCourse)
        {
            if (id != updatedCourse.CourseID)
            {
                return BadRequest("El ID del curso no coincide.");
            }

            // Verifica si el profesor asignado existe en la base de datos
            var teacherExists = await _context.Users.AnyAsync(t => t.UserID == updatedCourse.UserID);
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
