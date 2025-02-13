using Microsoft.AspNetCore.Mvc;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using Microsoft.EntityFrameworkCore;


namespace SchoolProyectBackend.Controllers
{
    [Route("api/grades")]
    [ApiController]
    public class GradeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Grade>>> GetGrades()
        {
            return await _context.Grades.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Grade>> GetGrade(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return NotFound();
            return grade;
        }

        [HttpPost]
        public async Task<ActionResult<Grade>> CreateGrade(Grade grade)
        {
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGrade), new { id = grade.GradeID}, grade);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return NotFound();
            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}