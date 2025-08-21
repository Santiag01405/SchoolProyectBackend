// Controllers/ExtracurricularController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.DTOs;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/extracurriculars")]
[ApiController]
public class ExtracurricularController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ExtracurricularController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExtracurricularActivity>>> GetExtracurricularActivities([FromQuery] int schoolId)
    {
        var activities = await _context.ExtracurricularActivities
            .Where(a => a.SchoolID == schoolId)
            .ToListAsync();
        return Ok(activities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExtracurricularActivity>> GetExtracurricularActivity(int id, [FromQuery] int schoolId)
    {
        var activity = await _context.ExtracurricularActivities
            .Where(a => a.ActivityID == id && a.SchoolID == schoolId)
            .FirstOrDefaultAsync();

        if (activity == null)
            return NotFound("Actividad no encontrada o no pertenece a este colegio.");

        return Ok(activity);
    }

    [HttpPost("create")]
    public async Task<ActionResult<ExtracurricularActivity>> CreateExtracurricularActivity([FromBody] ExtracurricularActivityCreateDto activityDto)
    {
        if (activityDto.UserID.HasValue && !await _context.Users.AnyAsync(u => u.UserID == activityDto.UserID && u.SchoolID == activityDto.SchoolID))
            return BadRequest("El usuario asignado no es válido para este colegio.");

        var activity = new ExtracurricularActivity
        {
            Name = activityDto.Name,
            Description = activityDto.Description,
            DayOfWeek = activityDto.DayOfWeek,
            UserID = activityDto.UserID,
            SchoolID = activityDto.SchoolID
        };

        _context.ExtracurricularActivities.Add(activity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExtracurricularActivity), new { id = activity.ActivityID, schoolId = activity.SchoolID }, activity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExtracurricularActivity(int id, [FromBody] ExtracurricularActivityCreateDto activityDto)
    {
        // Buscamos la actividad existente en la base de datos
        var activityToUpdate = await _context.ExtracurricularActivities
            .FirstOrDefaultAsync(a => a.ActivityID == id && a.SchoolID == activityDto.SchoolID);

        if (activityToUpdate == null)
        {
            return NotFound("Actividad no encontrada o no pertenece a este colegio.");
        }

        // Validamos que el profesor asignado (si se cambia) sea válido
        if (activityDto.UserID.HasValue && !await _context.Users.AnyAsync(u => u.UserID == activityDto.UserID && u.SchoolID == activityDto.SchoolID))
        {
            return BadRequest("El usuario asignado no es válido para este colegio.");
        }

        // Actualizamos las propiedades de la actividad con los nuevos datos
        activityToUpdate.Name = activityDto.Name;
        activityToUpdate.Description = activityDto.Description;
        activityToUpdate.DayOfWeek = activityDto.DayOfWeek;
        activityToUpdate.UserID = activityDto.UserID;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Esto maneja casos excepcionales donde el dato podría haber sido borrado por otro usuario
            // mientras se intentaba editar.
            if (!_context.ExtracurricularActivities.Any(e => e.ActivityID == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent(); // Código 204: Éxito sin devolver contenido
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExtracurricularActivity(int id, [FromQuery] int schoolId)
    {
        // 1. Buscamos la actividad para asegurarnos de que existe y pertenece a la escuela
        var activity = await _context.ExtracurricularActivities
            .Where(a => a.ActivityID == id && a.SchoolID == schoolId)
            .FirstOrDefaultAsync();

        if (activity == null)
        {
            return NotFound("Actividad no encontrada para esta escuela.");
        }

        // 2. Buscamos todas las inscripciones relacionadas con esta actividad y las eliminamos
        var enrollments = await _context.ExtracurricularEnrollments
            .Where(e => e.ActivityID == id)
            .ToListAsync();

        _context.ExtracurricularEnrollments.RemoveRange(enrollments);

        // 3. Ahora que las inscripciones ya no existen, podemos eliminar la actividad
        _context.ExtracurricularActivities.Remove(activity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Esto captura cualquier error de base de datos
            // Puedes loggear el 'ex.InnerException' para obtener más detalles del error real
            return StatusCode(500, "Error al eliminar la actividad. Asegúrate de que no existan otras relaciones no gestionadas.");
        }

        // Retornamos un código 204 para indicar que la eliminación fue exitosa
        return NoContent();
    }
}