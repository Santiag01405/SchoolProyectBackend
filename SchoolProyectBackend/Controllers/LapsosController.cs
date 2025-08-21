using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/lapsos")]
public class LapsosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LapsosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🟢 GET: api/lapsos?schoolId=1
    // Obtiene todos los lapsos de una escuela.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Lapso>>> GetLapsosBySchool([FromQuery] int schoolId)
    {
        var lapsos = await _context.Lapsos
            .Where(l => l.SchoolID == schoolId)
            .OrderBy(l => l.FechaInicio)
            .ToListAsync();

        return Ok(lapsos);
    }

    // 🟢 GET: api/lapsos/1
    // Obtiene un lapso por su ID.
    [HttpGet("{id}")]
    public async Task<ActionResult<Lapso>> GetLapsoById(int id)
    {
        var lapso = await _context.Lapsos.FindAsync(id);

        if (lapso == null)
        {
            return NotFound("Lapso no encontrado.");
        }
        return Ok(lapso);
    }

    // 🟢 POST: api/lapsos
    // Crea un nuevo lapso.
    [HttpPost]
    public async Task<ActionResult<Lapso>> CreateLapso(Lapso lapso)
    {
        _context.Lapsos.Add(lapso);
        await _context.SaveChangesAsync();
        return CreatedAtAction("GetLapsoById", new { id = lapso.LapsoID }, lapso);
    }

    // 🟢 PUT: api/lapsos/1
    // Actualiza un lapso existente.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLapso(int id, Lapso updatedLapso)
    {
        if (id != updatedLapso.LapsoID)
        {
            return BadRequest("El ID de la ruta no coincide con el ID del lapso.");
        }

        _context.Entry(updatedLapso).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Lapsos.Any(e => e.LapsoID == id))
            {
                return NotFound("Lapso no encontrado.");
            }
            else
            {
                throw;
            }
        }
        return Ok(new { message = "Lapso actualizado exitosamente." });
    }

    // 🟢 DELETE: api/lapsos/1
    // Elimina un lapso por su ID.
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLapso(int id)
    {
        var lapso = await _context.Lapsos.FindAsync(id);
        if (lapso == null)
        {
            return NotFound("Lapso no encontrado.");
        }

        _context.Lapsos.Remove(lapso);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Lapso eliminado exitosamente." });
    }
}