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
        /* [HttpGet]
         public async Task<ActionResult<IEnumerable<object>>> GetCourses()
         {
             var courses = await _context.Courses
                 .Select(c => new
                 {
                     c.CourseID,
                     c.Name,
                     c.Description,
                     DayOfWeek = c.DayOfWeek ?? 0,
                     UserID = c.UserID ?? 0 
                 })
                 .ToListAsync();

             return Ok(courses);
         }*/
        //Con schoolid
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCourses([FromQuery] int schoolId)
        {
            var courses = await _context.Courses
                .Where(c => c.SchoolID == schoolId)
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    c.Description,
                    DayOfWeek = c.DayOfWeek ?? 0,
                    UserID = c.UserID ?? 0
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






        // 2. Obtener un curso por ID
        /* [HttpGet("{id}")]
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
         }*/

        //Con schoolid
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCourse(int id, [FromQuery] int schoolId)
        {
            var course = await _context.Courses
                .Where(c => c.CourseID == id && c.SchoolID == schoolId)
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    Description = c.Description ?? "Sin descripción",
                    DayOfWeek = c.DayOfWeek ?? 0,
                    UserID = c.UserID ?? 0
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound(new { message = "Curso no encontrado o no pertenece al colegio" });

            return Ok(course);
        }



        // Crear un curso con `UserID` en lugar de `TeacherID`
        /* [HttpPost("create")]
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
         }*/

        //Con schoolid
        [HttpPost("create")]
        public async Task<ActionResult<Course>> CreateCourse([FromBody] Course courseRequest)
        {
            if (courseRequest == null || courseRequest.UserID == 0 || courseRequest.SchoolID == 0)
                return BadRequest("UserID y SchoolID son obligatorios.");

            var isTeacher = await _context.Users.AnyAsync(u =>
                u.UserID == courseRequest.UserID &&
                u.RoleID == 2 &&
                u.SchoolID == courseRequest.SchoolID);

            if (!isTeacher)
                return BadRequest("El usuario especificado no es un profesor válido en este colegio.");

            var schoolExists = await _context.Schools.AnyAsync(s => s.SchoolID == courseRequest.SchoolID);
            if (!schoolExists)
                return BadRequest("El SchoolID no existe.");

            var course = new Course
            {
                Name = courseRequest.Name,
                Description = courseRequest.Description,
                UserID = courseRequest.UserID,
                DayOfWeek = courseRequest.DayOfWeek,
                SchoolID = courseRequest.SchoolID
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseID, schoolId = course.SchoolID }, course);
        }



        // 3. Crear un nuevo curso
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

        // Obtener los cursos en los que un usuario es profesor
        /*  [HttpGet("user/{userId}/taught-courses")]
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
          }*/

        //Con schoolid
        [HttpGet("user/{userId}/taught-courses")]
        public async Task<IActionResult> GetCoursesByTeacher(int userId, [FromQuery] int schoolId)
        {
            var courses = await _context.Courses
                .Where(c => c.UserID == userId && c.SchoolID == schoolId)
                .Select(c => new
                {
                    c.CourseID,
                    c.Name,
                    Description = c.Description ?? "Sin descripción",
                    DayOfWeek = c.DayOfWeek ?? 0
                })
                .ToListAsync();

            if (courses == null || courses.Count == 0)
                return NotFound("No se encontraron cursos para este usuario en este colegio.");

            return Ok(courses);
        }



        // 4. Actualizar un curso existente
        /*  [HttpPut("{id}")]
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
          }*/

        //Con schoolid
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course updatedCourse)
        {
            if (id != updatedCourse.CourseID)
                return BadRequest("El ID del curso no coincide.");

            var teacherExists = await _context.Users.AnyAsync(u =>
                u.UserID == updatedCourse.UserID &&
                u.RoleID == 2 &&
                u.SchoolID == updatedCourse.SchoolID);

            if (!teacherExists)
                return BadRequest("El profesor especificado no es válido para ese colegio.");

            var schoolExists = await _context.Schools.AnyAsync(s => s.SchoolID == updatedCourse.SchoolID);
            if (!schoolExists)
                return BadRequest("El SchoolID no existe.");

            _context.Entry(updatedCourse).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // 5. Eliminar un curso
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                //.Include(c => c.Grades)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound();

            // 1. Eliminar inscripciones asociadas
            _context.Enrollments.RemoveRange(course.Enrollments);

            // 2. Eliminar notas asociadas
          //  if (course.Grades != null)
            //    _context.Grades.RemoveRange(course.Grades);

            // 3. Eliminar el curso
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /*  [HttpDelete("{id}")]
          public async Task<IActionResult> DeleteCourse(int id)
          {
              var course = await _context.Courses.FindAsync(id);
              if (course == null) return NotFound();

              _context.Courses.Remove(course);
              await _context.SaveChangesAsync();

              return NoContent();
          }*/


        [HttpGet("active-count")]
        public ActionResult<int> GetActiveCoursesCount([FromQuery] int schoolId)
        {
            return _context.Courses.Count(c => c.SchoolID == schoolId);
        }

        [HttpPut("{courseId}/assign-classroom/{classroomId}")]
        public async Task<IActionResult> AssignCourseToClassroom(int courseId, int classroomId)
        {
            try
            {
                Console.WriteLine($"📌 Intentando asignar curso {courseId} al salón {classroomId}");

                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                    return NotFound("Curso no encontrado.");

                var classroom = await _context.Classrooms.FindAsync(classroomId);
                if (classroom == null)
                    return NotFound("Salón no encontrado.");

                if (course.SchoolID != classroom.SchoolID)
                    return BadRequest("El curso y el salón no pertenecen al mismo colegio.");

                course.ClassroomID = classroomId;
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Curso {course.Name} asignado al salón {classroom.Name}.");
                return Ok(new { message = $"Curso '{course.Name}' asignado al salón '{classroom.Name}' correctamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AssignCourseToClassroom: {ex.Message}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


    }
}
