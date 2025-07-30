using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Obtener todas las inscripciones

        /* [HttpGet]
         public async Task<ActionResult<IEnumerable<object>>> GetAllEnrollments()
         {
             var enrollments = await _context.Enrollments
                 .Select(e => new
                 {
                     e.EnrollmentID,
                     e.UserID,
                     UserName = e.User != null ? e.User.UserName : "Usuario no encontrado",
                     e.CourseID,
                     CourseName = e.Course != null ? e.Course.Name : "Curso no encontrado",
                     StudentID = e.GetType().GetProperty("studentID") != null ? e.GetType().GetProperty("studentID")!.GetValue(e, null) : "Sin información"
                 })
                 .ToListAsync();

             return Ok(enrollments);
         }*/

        //Con schoolid
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllEnrollments([FromQuery] int schoolId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.SchoolID == schoolId)
                .Include(e => e.User)
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.EnrollmentID,
                    e.UserID,
                    UserName = e.User != null ? e.User.UserName : "Usuario no encontrado",
                    e.CourseID,
                    CourseName = e.Course != null ? e.Course.Name : "Curso no encontrado",
                    StudentID = e.GetType().GetProperty("studentID") != null ? e.GetType().GetProperty("studentID")!.GetValue(e, null) : "Sin información"
                })
                .ToListAsync();

            return Ok(enrollments);
        }


        [HttpGet("course/{courseID}/students")]
        public async Task<IActionResult> GetStudentsByCourse(int courseID)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseID == courseID)
                .Include(e => e.User) 
                .ToListAsync();

            Console.WriteLine($"🔍 Inscripciones encontradas para el curso {courseID}: {enrollments.Count}");

            if (enrollments.Count == 0)
            {
                return NotFound($"⚠️ No hay estudiantes inscritos en el curso con ID {courseID}");
            }

            var students = enrollments.Select(e => new
            {
                e.UserID,
                StudentName = e.User != null ? e.User.UserName : "Usuario no encontrado"
            }).ToList();

            return Ok(students);
        }

        // 2. Obtener todas las inscripciones de un usuario específico

        /* [HttpGet("user/{userId}")]
         public async Task<ActionResult<IEnumerable<object>>> GetUserEnrollments(int userId)
         {
             var enrollments = await _context.Enrollments
                 .Where(e => e.UserID == userId)
                 .Select(e => new
                 {
                     e.EnrollmentID,
                     e.UserID,
                     UserName = e.User != null ? e.User.UserName : "Usuario no encontrado",
                     e.CourseID,
                     CourseName = e.Course != null ? e.Course.Name : "Curso no encontrado",
                     StudentID = e.GetType().GetProperty("StudentID") != null
                         ? (e.GetType().GetProperty("StudentID")!.GetValue(e, null) ?? "Sin información")
                         : "No aplica"
                 })
                 .ToListAsync();

             if (enrollments == null || enrollments.Count == 0)
                 return NotFound("El usuario no tiene inscripciones.");

             return Ok(enrollments);
         }*/

        //Con schoolid
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserEnrollments(int userId, [FromQuery] int schoolId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.UserID == userId && e.SchoolID == schoolId)
                .Include(e => e.User)
                .Include(e => e.Course)
                .ToListAsync(); // ✅ Ejecutar primero la consulta

            var result = enrollments.Select(e => new
            {
                e.EnrollmentID,
                e.UserID,
                UserName = e.User != null ? e.User.UserName : "Usuario no encontrado",
                e.CourseID,
                CourseName = e.Course != null ? e.Course.Name : "Curso no encontrado"
            }).ToList();

            if (!result.Any())
                return NotFound("No hay inscripciones para este usuario en este colegio.");

            return Ok(result);
        }



        //Obtener todos los dias de la semana
        /* [HttpGet("user/{userId}/schedule")]
         public async Task<ActionResult<IEnumerable<object>>> GetUserWeeklySchedule(int userId) 
         {
             var enrollments = await _context.Enrollments
                 .Where(e => e.UserID == userId) // ✅ Asegurar que UserID es una propiedad
                 .Select(e => new
                 {
                     e.EnrollmentID,
                     e.UserID,
                     UserName = e.User != null ? e.User.UserName : "Usuario no encontrado",
                     e.CourseID,
                     CourseName = e.Course != null ? e.Course.Name : "Curso no encontrado",
                     DayOfWeek = e.Course != null ? (DayOfWeek?)e.Course.DayOfWeek : null,
                     StudentID = e.GetType().GetProperty("StudentID") != null
                         ? (e.GetType().GetProperty("StudentID")!.GetValue(e, null) ?? "Sin información")
                         : "No aplica"
                 })
                 .ToListAsync();

             if (enrollments == null || enrollments.Count == 0)
                 return NotFound("El usuario no tiene inscripciones.");

             return Ok(enrollments);
         }*/

        //Con schoolid
        [HttpGet("user/{userId}/schedule")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserWeeklySchedule(int userId, [FromQuery] int schoolId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.UserID == userId && e.SchoolID == schoolId)
                .Include(e => e.Course)
                .Include(e => e.User)
                .ToListAsync();

            var result = enrollments.Select(e => new
            {
                e.EnrollmentID,
                e.UserID,
                UserName = e.User?.UserName ?? "Usuario no encontrado",
                e.CourseID,
                CourseName = e.Course?.Name ?? "Curso no encontrado",
                DayOfWeek = e.Course?.DayOfWeek ?? 0
            }).ToList();

            if (!result.Any())
                return NotFound("El usuario no tiene inscripciones en este colegio.");

            return Ok(result);
        }


        // Obtener los estudiantes inscritos en los cursos de un profesor
        [HttpGet("user/{userId}/students")]
        public async Task<IActionResult> GetStudentsByTeacher(int userId)
        {
            // Obtener los CourseID en los que el usuario es profesor
            var courseIds = await _context.Courses
                .Where(c => _context.UserRelationships
                    .Any(ur => ur.User1ID == userId && ur.RelationshipType == "Profesor-Estudiante"))
                .Select(c => c.CourseID)
                .ToListAsync();

            if (courseIds.Count == 0)
                return NotFound("Este profesor no tiene cursos asignados.");

            // Obtener los estudiantes inscritos en esos cursos
            var students = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseID))
                .Select(e => new
                {
                    e.UserID,
                    StudentName = e.User.UserName,
                    e.CourseID,
                    CourseName = e.Course.Name
                })
                .ToListAsync();

            if (students.Count == 0)
                return NotFound("No hay estudiantes inscritos en estos cursos.");

            return Ok(students);
        }




        // 3. Asignar un usuario a un curso (crear inscripción)
        /* [HttpPost]
         public async Task<IActionResult> AssignUserToCourse([FromBody] Enrollment enrollment)
         {
             if (enrollment == null || enrollment.UserID == 0 || enrollment.CourseID == 0)
                 return BadRequest("UserID y CourseID son obligatorios.");

             // 🔹 Verificar si el usuario existe
             var userExists = await _context.Users.AnyAsync(u => u.UserID == enrollment.UserID);
             if (!userExists)
                 return NotFound("El usuario no existe.");

             // 🔹 Verificar si el curso existe
             var courseExists = await _context.Courses.AnyAsync(c => c.CourseID == enrollment.CourseID);
             if (!courseExists)
                 return NotFound("El curso no existe.");

             // 🔹 Verificar si ya está inscrito
             var existingEnrollment = await _context.Enrollments
                 .FirstOrDefaultAsync(e => e.UserID == enrollment.UserID && e.CourseID == enrollment.CourseID);

             if (existingEnrollment != null)
                 return BadRequest("El usuario ya está inscrito en este curso.");

             _context.Enrollments.Add(enrollment);

             try
             {
                 await _context.SaveChangesAsync();
                 return Ok(new { message = "Usuario inscrito en el curso correctamente." });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, $"Error interno del servidor: {ex.Message}");
             }
         }*/

        //Con schoolid
        [HttpPost]
        public async Task<IActionResult> AssignUserToCourse([FromBody] Enrollment enrollment)
        {
            if (enrollment == null || enrollment.UserID == 0 || enrollment.CourseID == 0 || enrollment.SchoolID == 0)
                return BadRequest("UserID, CourseID y SchoolID son obligatorios.");

            var user = await _context.Users.FindAsync(enrollment.UserID);
            if (user == null || user.SchoolID != enrollment.SchoolID)
                return BadRequest("El usuario no pertenece a esta escuela.");

            var course = await _context.Courses.FindAsync(enrollment.CourseID);
            if (course == null || course.SchoolID != enrollment.SchoolID)
                return BadRequest("El curso no pertenece a esta escuela.");

            var exists = await _context.Enrollments
                .AnyAsync(e => e.UserID == enrollment.UserID && e.CourseID == enrollment.CourseID);
            if (exists)
                return BadRequest("El usuario ya está inscrito en este curso.");

            _context.Enrollments.Add(enrollment);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Usuario inscrito correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }



        // 4. Modificar una inscripción existente (cambiar el curso de un usuario)
        /*[HttpPut("{id}")]
        public async Task<IActionResult> UpdateEnrollment(int id, [FromBody] Enrollment updatedEnrollment)
        {
            if (id != updatedEnrollment.EnrollmentID)
                return BadRequest("El ID de la inscripción no coincide.");

            var existingEnrollment = await _context.Enrollments.FindAsync(id);
            if (existingEnrollment == null)
                return NotFound("No se encontró la inscripción.");

            existingEnrollment.CourseID = updatedEnrollment.CourseID;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción actualizada correctamente." });
        }*/

        //Con schoolid
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEnrollment(int id, [FromBody] Enrollment updatedEnrollment)
        {
            if (id != updatedEnrollment.EnrollmentID)
                return BadRequest("El ID no coincide.");

            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
                return NotFound("Inscripción no encontrada.");

            var user = await _context.Users.FindAsync(updatedEnrollment.UserID);
            var course = await _context.Courses.FindAsync(updatedEnrollment.CourseID);

            if (user?.SchoolID != updatedEnrollment.SchoolID || course?.SchoolID != updatedEnrollment.SchoolID)
                return BadRequest("Usuario o curso no pertenecen al mismo colegio.");

            enrollment.UserID = updatedEnrollment.UserID;
            enrollment.CourseID = updatedEnrollment.CourseID;
            enrollment.SchoolID = updatedEnrollment.SchoolID;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción actualizada correctamente." });
        }


        // 5. Eliminar una inscripción
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
                return NotFound("No se encontró la inscripción.");

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción eliminada correctamente." });
        }
    }
}
