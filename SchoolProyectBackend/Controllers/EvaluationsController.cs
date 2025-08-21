using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/evaluations")]
    [ApiController]
    public class EvaluationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EvaluationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método para obtener el LapsoID basado en la fecha de la evaluación
        private async Task<int?> GetLapsoIdByDate(DateTime date, int schoolId)
        {
            var lapso = await _context.Lapsos
                .FirstOrDefaultAsync(l => l.SchoolID == schoolId && date >= l.FechaInicio && date <= l.FechaFin);

            return lapso?.LapsoID;
        }
       
        [HttpGet]
        public async Task<IActionResult> GetEvaluations([FromQuery] int userID, [FromQuery] int schoolId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userID);
                if (user == null || user.SchoolID != schoolId)
                {
                    return NotFound("Usuario no encontrado en este colegio.");
                }

                IQueryable<Evaluation> evaluationsQuery;

                if (user.RoleID == 2) // Si el usuario es un profesor
                {
                    evaluationsQuery = _context.Evaluations
                        .Where(e => e.UserID == userID && e.SchoolID == schoolId);
                }
                else // Si el usuario es un estudiante o tiene otro rol
                {
                    var userCourseIds = await _context.Enrollments
                        .Where(e => e.UserID == userID && e.SchoolID == schoolId)
                        .Select(e => e.CourseID)
                        .ToListAsync();

                    if (!userCourseIds.Any())
                    {
                        return NotFound("El usuario no está inscrito en ningún curso en este colegio.");
                    }

                    evaluationsQuery = _context.Evaluations
                        .Where(e => userCourseIds.Contains(e.CourseID) && e.SchoolID == schoolId);
                }

                // ✅ CORRECCIÓN: Usar .Include() para cargar los datos relacionados
                var evaluations = await evaluationsQuery
                    .Include(e => e.Course) // Incluye el objeto Course completo
                    .Include(e => e.Lapso)  // Incluye el objeto Lapso completo
                    .OrderBy(e => e.Date)
                    .ToListAsync();

                if (!evaluations.Any())
                {
                    return NotFound("No se encontraron evaluaciones para este usuario en este colegio.");
                }

                return Ok(evaluations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en GetEvaluations: {ex.Message}");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Evaluation>> GetEvaluation(int id, int schoolId)
        {
            var evaluation = await _context.Evaluations.FirstOrDefaultAsync(e => e.EvaluationID == id && e.SchoolID == schoolId);

            if (evaluation == null)
            {
                return NotFound(); // Esto generaría un error 404
            }

            return evaluation; // Esto debería funcionar si el objeto existe
        }

        /*  [HttpPost]
          public async Task<IActionResult> PostEvaluation([FromBody] Evaluation evaluation)
          {
              try
              {
                  if (evaluation == null)
                      return BadRequest("Datos de evaluación inválidos.");

                  var user = await _context.Users.FindAsync(evaluation.UserID);
                  if (user == null || user.SchoolID != evaluation.SchoolID)
                      return BadRequest("El usuario no existe o no pertenece a esta escuela.");

                  var course = await _context.Courses.FindAsync(evaluation.CourseID);
                  if (course == null || course.SchoolID != evaluation.SchoolID)
                      return BadRequest("El curso especificado no existe o no pertenece a esta escuela.");

                  // ✅ Asignar ClassroomID a la evaluación
                  if (course.ClassroomID.HasValue)
                  {
                      evaluation.ClassroomID = course.ClassroomID;
                  }
                  else
                  {
                      return BadRequest("El curso no tiene un salón de clases asignado.");
                  }

                  _context.Evaluations.Add(evaluation);
                  await _context.SaveChangesAsync();

                  // 🚀 Lógica de notificación a todos los estudiantes del salón y a sus padres
                  await NotifyClassroomForNewEvaluation(evaluation, course);

                  return CreatedAtAction(nameof(GetEvaluations),
                      new { userID = evaluation.UserID, schoolId = evaluation.SchoolID }, evaluation);
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
                  return StatusCode(500, "Error interno del servidor.");
              }
          }*/

        [HttpPost]
        public async Task<IActionResult> PostEvaluation([FromBody] Evaluation evaluation)
        {
            try
            {
                if (evaluation == null)
                    return BadRequest("Datos de evaluación inválidos.");

                var user = await _context.Users.FindAsync(evaluation.UserID);
                if (user == null || user.SchoolID != evaluation.SchoolID)
                    return BadRequest("El usuario no existe o no pertenece a esta escuela.");

                var course = await _context.Courses.FindAsync(evaluation.CourseID);
                if (course == null || course.SchoolID != evaluation.SchoolID)
                    return BadRequest("El curso especificado no existe o no pertenece a esta escuela.");

                if (course.ClassroomID.HasValue)
                {
                    evaluation.ClassroomID = course.ClassroomID;
                }
                else
                {
                    return BadRequest("El curso no tiene un salón de clases asignado.");
                }

                // ✅ Buscar y asignar el LapsoID
                var lapsoId = await GetLapsoIdByDate(evaluation.Date, evaluation.SchoolID);
                if (!lapsoId.HasValue)
                {
                    return BadRequest("No se encontró un lapso válido para la fecha de la evaluación.");
                }
                evaluation.LapsoID = lapsoId.Value;

                _context.Evaluations.Add(evaluation);
                await _context.SaveChangesAsync();
                await NotifyClassroomForNewEvaluation(evaluation, course);

                return CreatedAtAction(nameof(GetEvaluations),
                    new { userID = evaluation.UserID, schoolId = evaluation.SchoolID }, evaluation);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        private async Task NotifyClassroomForNewEvaluation(Evaluation evaluation, Course course)
        {
            // 1. Encontramos a todos los estudiantes inscritos en el curso y en el mismo salón
            var enrolledStudents = await _context.Enrollments
                .Where(e => e.CourseID == evaluation.CourseID && e.User.ClassroomID == evaluation.ClassroomID)
                .Include(e => e.User)
                .Select(e => e.User)
                .ToListAsync();

            if (!enrolledStudents.Any())
            {
                return; // No hay estudiantes inscritos en este salón.
            }

            // 2. Recorremos cada estudiante para crear notificaciones individuales
            foreach (var student in enrolledStudents)
            {
                // 🔹 Notificación para el estudiante
                var studentNotification = new Notification
                {
                    UserID = student.UserID,
                    Title = "Nueva Evaluación Asignada",
                    Content = $"Se ha asignado una nueva evaluación: '{evaluation.Title}' en el curso de {course.Name}.",
                    IsRead = false,
                    Date = DateTime.Now,
                    SchoolID = evaluation.SchoolID
                };
                _context.Notifications.Add(studentNotification);

                // 🔹 Notificación para los padres del estudiante
                var parents = await _context.UserRelationships
                    .Where(ur => ur.User1ID == student.UserID && ur.RelationshipType == "Padre-Hijo")
                    .Select(ur => ur.User2ID)
                    .ToListAsync();

                foreach (var parentId in parents)
                {
                    var parentNotification = new Notification
                    {
                        UserID = parentId,
                        Title = "Nueva Evaluación Asignada",
                        Content = $"Se ha asignado una nueva evaluación: '{evaluation.Title}' para tu hijo/a **{student.UserName}** en el curso de {course.Name}.",
                        IsRead = false,
                        Date = DateTime.Now,
                        SchoolID = evaluation.SchoolID
                    };
                    _context.Notifications.Add(parentNotification);
                }
            }

            await _context.SaveChangesAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvaluation(int id, Evaluation updatedEvaluation)
        {
            var evaluation = await _context.Evaluations.FindAsync(id);
            if (evaluation == null)
                return NotFound(new { message = "Evaluación no encontrada." });

            var user = await _context.Users.FindAsync(updatedEvaluation.UserID);
            var course = await _context.Courses.FindAsync(updatedEvaluation.CourseID);

            if (user == null || user.RoleID != 2 || user.SchoolID != updatedEvaluation.SchoolID)
                return Unauthorized(new { message = "Solo el profesor responsable de esta escuela puede modificarla." });

            if (course == null || course.SchoolID != updatedEvaluation.SchoolID)
                return BadRequest("El curso no pertenece a la misma escuela.");

            evaluation.Title = updatedEvaluation.Title;
            evaluation.Description = updatedEvaluation.Description;
            evaluation.Date = updatedEvaluation.Date;
            evaluation.CourseID = updatedEvaluation.CourseID;
            evaluation.SchoolID = updatedEvaluation.SchoolID;
            evaluation.LapsoID = updatedEvaluation.LapsoID;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvaluation(int id, [FromQuery] int schoolId)
        {
            // 1. Cargar la evaluación y verificar su existencia
            var evaluation = await _context.Evaluations.FindAsync(id);
            if (evaluation == null || evaluation.SchoolID != schoolId)
                return NotFound(new { message = "Evaluación no encontrada en esta escuela." });

            // ✅ La restricción de permisos ha sido eliminada.

            // 2. Buscar y eliminar todas las calificaciones relacionadas.
            var gradesToDelete = await _context.Grades
                .Where(g => g.EvaluationID == id)
                .ToListAsync();

            if (gradesToDelete.Any())
            {
                _context.Grades.RemoveRange(gradesToDelete);
                await _context.SaveChangesAsync();
            }

            // 3. Ahora que no hay calificaciones asociadas, eliminar la evaluación.
            _context.Evaluations.Remove(evaluation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}