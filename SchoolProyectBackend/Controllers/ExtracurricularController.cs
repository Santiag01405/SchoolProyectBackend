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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExtracurricularActivity(int id, [FromQuery] int schoolId)
    {
        var activity = await _context.ExtracurricularActivities
            .Where(a => a.ActivityID == id && a.SchoolID == schoolId)
            .FirstOrDefaultAsync();

        if (activity == null)
            return NotFound("Actividad no encontrada para esta escuela.");

        _context.ExtracurricularActivities.Remove(activity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}