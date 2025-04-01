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

        // ✅ Obtener evaluaciones por UserID (Estudiantes y padres pueden verlas)

        [HttpGet]  // 👈 Esto permite /api/evaluations?userID=61 en lugar de /api/evaluations/61
        public async Task<IActionResult> GetEvaluations([FromQuery] int userID)
        {
            try
            {
                Console.WriteLine($"📌 Buscando evaluaciones para UserID: {userID}");

                var evaluations = await _context.Evaluations
                    .Where(e => e.UserID == userID)
                    .OrderBy(e => e.Date)  // 👈 Ordenar por fecha
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
        }


// ✅ Crear una nueva evaluación (Solo profesores pueden)
[HttpPost]
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

                // 2️⃣ Validar que el usuario sea un profesor (RoleID = 2)
               if (user.RoleID != 2)
                {
                   return StatusCode(403, "Solo los profesores pueden crear evaluaciones."); 
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
        }


        // ✅ Editar evaluación (Solo el profesor que la creó puede)
        [HttpPut("{id}")]
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
        }

        // ✅ Eliminar evaluación (Solo el profesor que la creó puede)
        [HttpDelete("{id}")]
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

        // 📌 Obtener evaluaciones de un usuario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Evaluation>>> GetEvaluations(int userID)
        {
            var evaluations = await _context.Evaluations
                .Where(e => e.UserID == userID)
                .OrderBy(e => e.Date)
                .ToListAsync();

            if (!evaluations.Any())
            {
                return NotFound(new { message = "No hay evaluaciones asignadas." });
            }

            return evaluations;
        }

        // 📌 Crear una evaluación (Solo profesores)
        [HttpPost]
        public async Task<ActionResult<Evaluation>> CreateEvaluation(Evaluation evaluation)
        {
            var user = await _context.Users.FindAsync(evaluation.UserID);

            if (user == null || user.RoleID != 2) // Solo profesores pueden crear evaluaciones
            {
                return Forbid("Solo los profesores pueden crear evaluaciones.");
            }

            if (evaluation.Date < DateTime.Now)
            {
                return BadRequest(new { message = "No se pueden asignar evaluaciones con fechas pasadas." });
            }

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvaluations), new { userID = evaluation.UserID }, evaluation);
        }

        // 📌 Editar una evaluación
        [HttpPut("{id}")]
        public async Task<IActionResult> EditEvaluation(int id, Evaluation updatedEvaluation)
        {
            var evaluation = await _context.Evaluations.FindAsync(id);

            if (evaluation == null)
            {
                return NotFound(new { message = "Evaluación no encontrada." });
            }

            if (updatedEvaluation.Date < DateTime.Now)
            {
                return BadRequest(new { message = "No se pueden asignar evaluaciones con fechas pasadas." });
            }

            evaluation.Title = updatedEvaluation.Title;
            evaluation.Description = updatedEvaluation.Description;
            evaluation.Date = updatedEvaluation.Date;
            evaluation.CourseID = updatedEvaluation.CourseID;
            evaluation.UserID = updatedEvaluation.UserID;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 📌 Eliminar una evaluación
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvaluation(int id)
        {
            var evaluation = await _context.Evaluations.FindAsync(id);

            if (evaluation == null)
            {
                return NotFound(new { message = "Evaluación no encontrada." });
            }

            _context.Evaluations.Remove(evaluation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}*/