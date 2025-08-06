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

        [HttpGet]
        public async Task<IActionResult> GetEvaluations([FromQuery] int userID, [FromQuery] int schoolId)
        {
            try
            {
                // 🔹 Paso 1: Identificar el rol del usuario
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userID);
                if (user == null || user.SchoolID != schoolId)
                {
                    return NotFound("Usuario no encontrado en este colegio.");
                }

                IQueryable<Evaluation> evaluationsQuery;

                if (user.RoleID == 2) // Si el usuario es un profesor
                {
                    // ✅ Lógica actual: buscar evaluaciones creadas por este profesor
                    evaluationsQuery = _context.Evaluations
                        .Where(e => e.UserID == userID && e.SchoolID == schoolId);
                }
                else // Si el usuario es un estudiante o tiene otro rol
                {
                    // ✅ Nueva lógica: buscar evaluaciones de los cursos en los que está inscrito
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

                var evaluations = await evaluationsQuery
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

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvaluation(int id, [FromQuery] int userID, [FromQuery] int schoolId)
        {
            var evaluation = await _context.Evaluations.FindAsync(id);
            if (evaluation == null || evaluation.SchoolID != schoolId)
                return NotFound(new { message = "Evaluación no encontrada en esta escuela." });

            var user = await _context.Users.FindAsync(userID);
            if (user == null || user.RoleID != 2 || user.SchoolID != schoolId)
                return Unauthorized(new { message = "Solo el profesor de esta escuela puede eliminarla." });

            _context.Evaluations.Remove(evaluation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

/*using Microsoft.AspNetCore.Mvc;
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



        /* [HttpGet] 
         public async Task<IActionResult> GetEvaluations([FromQuery] int userID)
         {
             try
             {
                 Console.WriteLine($"📌 Buscando evaluaciones para UserID: {userID}");

                 var evaluations = await _context.Evaluations
                     .Where(e => e.UserID == userID)
                     .OrderBy(e => e.Date)  
                     .ToListAsync();

                 if (!evaluations.Any())
                 {
                     return NotFound("Este usuario no tiene evaluaciones asignadas.");
                 }

                 return Ok(evaluations);
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"❌ Error en GetEvaluations: {ex.Message}");
                 return StatusCode(500, "Error interno del servidor.");
             }
         }*

        //Con schoolid
        [HttpGet]
        public async Task<IActionResult> GetEvaluations([FromQuery] int userID, [FromQuery] int schoolId)
        {
            try
            {
                Console.WriteLine($"📌 Buscando evaluaciones para UserID: {userID} en SchoolID: {schoolId}");

                var evaluations = await _context.Evaluations
                    .Where(e => e.UserID == userID && e.SchoolID == schoolId)
                    .OrderBy(e => e.Date)
                    .ToListAsync();

                if (!evaluations.Any())
                {
                    return NotFound("Este usuario no tiene evaluaciones asignadas en este colegio.");
                }

                return Ok(evaluations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en GetEvaluations: {ex.Message}");
                return StatusCode(500, "Error interno del servidor.");
            }
        }



        // Crear una nueva evaluación (Solo profesores pueden)
        /* [HttpPost]
         public async Task<IActionResult> PostEvaluation([FromBody] Evaluation evaluation)
         {
             try
             {
                 if (evaluation == null)
                 {
                     return BadRequest("Datos de evaluación inválidos.");
                 }

                 // 1️⃣ Verificar que el usuario existe
                 var user = await _context.Users.FindAsync(evaluation.UserID);
                 if (user == null)
                 {
                     return BadRequest("El usuario no existe.");
                 }


                 // 3️⃣ Verificar que el curso existe
                 var course = await _context.Courses.FindAsync(evaluation.CourseID);
                 if (course == null)
                 {
                     return BadRequest("El curso especificado no existe.");
                 }

                 // 4️⃣ Guardar la evaluación en la base de datos
                 _context.Evaluations.Add(evaluation);
                 await _context.SaveChangesAsync();

                 return CreatedAtAction(nameof(GetEvaluations), new { userID = evaluation.UserID }, evaluation);
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
                 return StatusCode(500, "Error interno del servidor.");
             }
         }*/

//Con schoolid
/*  [HttpPost]
  public async Task<IActionResult> PostEvaluation([FromBody] Evaluation evaluation)
  {
      try
      {
          if (evaluation == null)
          {
              return BadRequest("Datos de evaluación inválidos.");
          }

          var user = await _context.Users.FindAsync(evaluation.UserID);
          if (user == null || user.SchoolID != evaluation.SchoolID)
          {
              return BadRequest("El usuario no existe o no pertenece a esta escuela.");
          }

          var course = await _context.Courses.FindAsync(evaluation.CourseID);
          if (course == null || course.SchoolID != evaluation.SchoolID)
          {
              return BadRequest("El curso especificado no existe o no pertenece a esta escuela.");
          }

          _context.Evaluations.Add(evaluation);
          await _context.SaveChangesAsync();

          return CreatedAtAction(nameof(GetEvaluations), new { userID = evaluation.UserID, schoolId = evaluation.SchoolID }, evaluation);
      }
      catch (Exception ex)
      {
          Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
          return StatusCode(500, "Error interno del servidor.");
      }
  }
*/
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

          // ✅ Si el curso tiene salón asignado, agregamos ClassroomID
          if (course.ClassroomID.HasValue)
          {
              evaluation.ClassroomID = course.ClassroomID;
          }

          _context.Evaluations.Add(evaluation);
          await _context.SaveChangesAsync();

          return CreatedAtAction(nameof(GetEvaluations),
              new { userID = evaluation.UserID, schoolId = evaluation.SchoolID }, evaluation);
      }
      catch (Exception ex)
      {
          Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
          return StatusCode(500, "Error interno del servidor.");
      }
  }*


// ... (El resto de tu código del controlador)
// ... (El resto de tu código del controlador)

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

        _context.Evaluations.Add(evaluation);
        await _context.SaveChangesAsync();

        // 🚀 Lógica de notificación después de guardar
        await NotifyStudentsAndParentsForNewEvaluation(evaluation, course);

        return CreatedAtAction(nameof(GetEvaluations),
            new { userID = evaluation.UserID, schoolId = evaluation.SchoolID }, evaluation);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al crear evaluación: {ex.Message}");
        return StatusCode(500, "Error interno del servidor.");
    }
}

// 👇 Método corregido para notificar a estudiantes y padres sobre la nueva evaluación
private async Task NotifyStudentsAndParentsForNewEvaluation(Evaluation evaluation, Course course)
{
    // 1. Encontramos a todos los estudiantes inscritos en el curso, incluyendo sus datos de usuario
    var enrolledStudents = await _context.Enrollments
        .Where(e => e.CourseID == evaluation.CourseID)
        .Include(e => e.User)
        .Select(e => e.User)
        .ToListAsync();

    if (!enrolledStudents.Any())
    {
        return; // No hay estudiantes inscritos.
    }

    // 2. Creamos las notificaciones para cada estudiante individualmente
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
    }

    // 3. Obtenemos una lista de padres únicos y sus hijos en el curso
    var parentsWithChildren = new Dictionary<int, List<string>>();
    foreach (var student in enrolledStudents)
    {
        var parents = await _context.UserRelationships
            .Where(ur => ur.User1ID == student.UserID && ur.RelationshipType == "Padre-Hijo")
            .Select(ur => ur.User2ID)
            .ToListAsync();

        foreach (var parentId in parents)
        {
            if (!parentsWithChildren.ContainsKey(parentId))
            {
                parentsWithChildren[parentId] = new List<string>();
            }
            parentsWithChildren[parentId].Add(student.UserName);
        }
    }

    // 4. Creamos una única notificación por cada padre, consolidando la información
    foreach (var parentEntry in parentsWithChildren)
    {
        var parentId = parentEntry.Key;
        var childrenNames = string.Join(" y ", parentEntry.Value);
        var content = $"Se ha asignado una nueva evaluación: '{evaluation.Title}' para tu(s) hijo(s) **{childrenNames}** en el curso de {course.Name}.";

        var parentNotification = new Notification
        {
            UserID = parentId,
            Title = "Nueva Evaluación Asignada",
            Content = content,
            IsRead = false,
            Date = DateTime.Now,
            SchoolID = evaluation.SchoolID
        };
        _context.Notifications.Add(parentNotification);
    }

    await _context.SaveChangesAsync();
}


// ... (El resto de tu código del controlador)

// ... (El resto de tu código del controlador)

// Editar evaluación (Solo el profesor que la creó puede)
/* [HttpPut("{id}")]
public async Task<IActionResult> UpdateEvaluation(int id, Evaluation updatedEvaluation)
{
var evaluation = await _context.Evaluations.FindAsync(id);
if (evaluation == null)
 return NotFound(new { message = "Evaluación no encontrada." });

var user = await _context.Users.FindAsync(updatedEvaluation.UserID);
if (user == null || user.RoleID != 2)
 return Unauthorized(new { message = "Solo el profesor que la creó puede modificarla." });

// Actualizamos los datos
evaluation.Title = updatedEvaluation.Title;
evaluation.Description = updatedEvaluation.Description;
evaluation.Date = updatedEvaluation.Date;
evaluation.CourseID = updatedEvaluation.CourseID;

await _context.SaveChangesAsync();
return NoContent();
}*

//Con schoolid
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

    // Actualizar
    evaluation.Title = updatedEvaluation.Title;
    evaluation.Description = updatedEvaluation.Description;
    evaluation.Date = updatedEvaluation.Date;
    evaluation.CourseID = updatedEvaluation.CourseID;
    evaluation.SchoolID = updatedEvaluation.SchoolID;

    await _context.SaveChangesAsync();
    return NoContent();
}


// Eliminar evaluación (Solo el profesor que la creó puede)
/* [HttpDelete("{id}")]
 public async Task<IActionResult> DeleteEvaluation(int id, [FromQuery] int userID)
 {
     var evaluation = await _context.Evaluations.FindAsync(id);
     if (evaluation == null)
         return NotFound(new { message = "Evaluación no encontrada." });

     var user = await _context.Users.FindAsync(userID);
     if (user == null || user.RoleID != 2)
         return Unauthorized(new { message = "Solo el profesor que la creó puede eliminarla." });

     _context.Evaluations.Remove(evaluation);
     await _context.SaveChangesAsync();
     return NoContent();
 }

//Con schoolid
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteEvaluation(int id, [FromQuery] int userID, [FromQuery] int schoolId)
{
    var evaluation = await _context.Evaluations.FindAsync(id);
    if (evaluation == null || evaluation.SchoolID != schoolId)
        return NotFound(new { message = "Evaluación no encontrada en esta escuela." });

    var user = await _context.Users.FindAsync(userID);
    if (user == null || user.RoleID != 2 || user.SchoolID != schoolId)
        return Unauthorized(new { message = "Solo el profesor de esta escuela puede eliminarla." });

    _context.Evaluations.Remove(evaluation);
    await _context.SaveChangesAsync();
    return NoContent();
}


}
}*/
