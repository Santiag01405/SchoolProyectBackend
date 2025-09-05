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

        // Con schoolid
        // Modificado para incluir el nombre del profesor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCourses([FromQuery] int schoolId)
        {
            var courses = await _context.Courses
                // ✅ JOIN con la tabla de Users para obtener el nombre del profesor
                .Join(_context.Users,
                    course => course.UserID,
                    user => user.UserID,
                    (course, user) => new { Course = course, User = user })
                .Where(joined => joined.Course.SchoolID == schoolId)
                .Select(joined => new
                {
                    joined.Course.CourseID,
                    joined.Course.Name,
                    joined.Course.Description,
                    DayOfWeek = joined.Course.DayOfWeek ?? 0,
                    joined.Course.UserID,
                    // ✅ Se añade el nombre del profesor
                    TeacherName = joined.User.UserName
                })
                .ToListAsync();

            return Ok(courses);
        }

        // Con schoolid
        // Modificado para incluir el nombre del profesor
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCourse(int id, [FromQuery] int schoolId)
        {
            var course = await _context.Courses
                // ✅ JOIN con la tabla de Users para obtener el nombre del profesor
                .Join(_context.Users,
                    c => c.UserID,
                    u => u.UserID,
                    (c, u) => new { Course = c, User = u })
                .Where(joined => joined.Course.CourseID == id && joined.Course.SchoolID == schoolId)
                .Select(joined => new
                {
                    joined.Course.CourseID,
                    joined.Course.Name,
                    Description = joined.Course.Description ?? "Sin descripción",
                    DayOfWeek = joined.Course.DayOfWeek ?? 0,
                    joined.Course.UserID,
                    TeacherName = joined.User.UserName
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound(new { message = "Curso no encontrado o no pertenece al colegio" });

            return Ok(course);
        }

        // Con schoolid
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

        // Con schoolid
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

        // Con schoolid
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
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound();

            // 1. Eliminar inscripciones asociadas
            _context.Enrollments.RemoveRange(course.Enrollments);

            // 2. Eliminar el curso
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("active-count")]
        public ActionResult<int> GetActiveCoursesCount([FromQuery] int schoolId)
        {
            return _context.Courses.Count(c => c.SchoolID == schoolId);
        }

        // Lógica corregida para asignar un curso a un salón e inscribir a los estudiantes
        /*   [HttpPut("{courseId}/assign-classroom/{classroomId}")]
           public async Task<IActionResult> AssignCourseToClassroom(int courseId, int classroomId)
           {
               try
               {
                   Console.WriteLine($"📌 Intentando asignar curso {courseId} al salón {classroomId}");

                   var course = await _context.Courses
                                               .Include(c => c.Enrollments)
                                               .FirstOrDefaultAsync(c => c.CourseID == courseId);
                   if (course == null)
                       return NotFound("Curso no encontrado.");

                   var classroom = await _context.Classrooms.FindAsync(classroomId);
                   if (classroom == null)
                       return NotFound("Salón no encontrado.");

                   if (course.SchoolID != classroom.SchoolID)
                       return BadRequest("El curso y el salón no pertenecen al mismo colegio.");

                   if (course.ClassroomID.HasValue && course.ClassroomID != classroomId)
                   {
                       var oldEnrollments = await _context.Enrollments
                           .Where(e => e.CourseID == courseId)
                           .ToListAsync();
                       _context.Enrollments.RemoveRange(oldEnrollments);
                   }

                   course.ClassroomID = classroomId;

                   var studentsInClassroom = await _context.Users
                       .Where(u => u.ClassroomID == classroomId && u.RoleID == 1)
                       .ToListAsync();

                   var newEnrollments = new List<Enrollment>();
                   foreach (var student in studentsInClassroom)
                   {
                       var isAlreadyEnrolled = await _context.Enrollments
                           .AnyAsync(e => e.UserID == student.UserID && e.CourseID == courseId);

                       if (!isAlreadyEnrolled)
                       {
                           newEnrollments.Add(new Enrollment
                           {
                               UserID = student.UserID,
                               CourseID = courseId,
                               SchoolID = course.SchoolID
                           });
                       }
                   }

                   _context.Enrollments.AddRange(newEnrollments);
                   await _context.SaveChangesAsync();

                   Console.WriteLine($"✅ Curso '{course.Name}' asignado al salón '{classroom.Name}'. {newEnrollments.Count} estudiantes inscritos.");
                   return Ok(new { message = $"Curso '{course.Name}' asignado al salón '{classroom.Name}' y estudiantes inscritos correctamente." });
               }
               catch (Exception ex)
               {
                   Console.WriteLine($"❌ Error en AssignCourseToClassroom: {ex.Message}");
                   return StatusCode(500, $"Error interno del servidor: {ex.Message}");
               }
           }*/
        [HttpPut("{courseId}/assign-classroom/{classroomId}")]
        public async Task<IActionResult> AssignCourseToClassroom(int courseId, int classroomId)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId);
                if (course == null)
                    return NotFound("Curso no encontrado.");

                var classroom = await _context.Classrooms.FindAsync(classroomId);
                if (classroom == null)
                    return NotFound("Salón no encontrado.");

                if (course.SchoolID != classroom.SchoolID)
                    return BadRequest("El curso y el salón no pertenecen al mismo colegio.");

                // Eliminar TODAS las inscripciones existentes para el curso
                var existingEnrollments = await _context.Enrollments
                    .Where(e => e.CourseID == courseId)
                    .ToListAsync();
                _context.Enrollments.RemoveRange(existingEnrollments);

                // Actualizar el curso con el nuevo ID de salón
                course.ClassroomID = classroomId;

                // Obtener la lista de estudiantes del salón (como lo hace tu endpoint de lectura)
                var studentsInClassroom = await _context.Users
                    .Where(u => u.ClassroomID == classroomId && u.RoleID == 1)
                    .ToListAsync();

                // Crear nuevas inscripciones para CADA estudiante de esa lista
                var newEnrollments = studentsInClassroom.Select(student => new Enrollment
                {
                    UserID = student.UserID,
                    CourseID = courseId,
                    SchoolID = course.SchoolID
                }).ToList();

                _context.Enrollments.AddRange(newEnrollments);

                // Guardar todos los cambios en la base de datos de una vez
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Curso '{course.Name}' asignado al salón '{classroom.Name}' y estudiantes inscritos correctamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AssignCourseToClassroom: {ex.Message}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}

