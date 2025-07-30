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
         }*/

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
        }



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
         }*/

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
         }*/

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
}
