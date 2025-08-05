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

        [HttpGet("course/{courseId}/evaluations")]
        public async Task<IActionResult> GetEvaluationsByCourse(int courseId, [FromQuery] int classroomId, [FromQuery] int schoolId)
        {
            var evaluations = await _context.Evaluations
                .Where(e => e.CourseID == courseId && e.ClassroomID == classroomId && e.SchoolID == schoolId)
                .ToListAsync();

            if (!evaluations.Any())
                return NotFound("No hay evaluaciones para este curso y salón.");

            return Ok(evaluations);
        }

        [HttpGet("evaluation/{evaluationId}/students")]
        public async Task<IActionResult> GetStudentsByEvaluation(int evaluationId)
        {
            var evaluation = await _context.Evaluations.FindAsync(evaluationId);
            if (evaluation == null)
                return NotFound("Evaluación no encontrada.");

            var students = await _context.Enrollments
                .Where(e => e.CourseID == evaluation.CourseID)
                .Join(_context.Users,
                      enr => enr.UserID,
                      u => u.UserID,
                      (enr, u) => new { u.UserID, u.UserName, u.RoleID })
                .Where(u => u.RoleID == 1)
                .ToListAsync();

            return Ok(students);
        }


        [HttpPost("assign")]
        public async Task<IActionResult> AssignGrade([FromBody] Grade grade)
        {
            if (grade.UserID == 0 || grade.EvaluationID == null || grade.SchoolID == 0)
                return BadRequest("Datos incompletos.");

            // Buscar el usuario (estudiante)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == grade.UserID);

            // Buscar la evaluación
            var evaluation = await _context.Evaluations.FirstOrDefaultAsync(e => e.EvaluationID == grade.EvaluationID);

            if (user == null)
                return NotFound("Usuario no encontrado.");
            if (evaluation == null)
                return NotFound("Evaluación no encontrada.");

            // Validar escuela
            if (user.SchoolID != grade.SchoolID)
                return BadRequest($"El usuario pertenece a otra escuela ({user.SchoolID}).");

            if (evaluation.SchoolID != grade.SchoolID)
                return BadRequest($"La evaluación pertenece a otra escuela ({evaluation.SchoolID}).");

            // Validar si ya existe la nota
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.UserID == grade.UserID && g.EvaluationID == grade.EvaluationID);

            if (existingGrade != null)
            {
                existingGrade.GradeValue = grade.GradeValue;
                existingGrade.Comments = grade.Comments;
            }
            else
            {
                _context.Grades.Add(grade);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Calificación registrada correctamente." });
        }


        [HttpGet("user/{userId}/grades")]
        public async Task<IActionResult> GetGradesForUser(int userId, [FromQuery] int schoolId)
        {
            // Buscar usuario
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.SchoolID == schoolId);
            if (user == null)
                return NotFound("Usuario no encontrado en esta escuela.");

            List<int> usersToSearch = new();

            if (user.RoleID == 1) // 1 = Estudiante
            {
                usersToSearch.Add(userId);
            }
            else
            {
                // Buscar hijos si el usuario es padre
                var children = await _context.UserRelationships
                    .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo" && ur.SchoolID == schoolId)
                    .Select(ur => ur.User1ID)
                    .ToListAsync();

                if (!children.Any())
                    return NotFound("No se encontraron hijos asociados a este usuario en esta escuela.");

                usersToSearch.AddRange(children);
            }

            // Obtener calificaciones
            var grades = await _context.Grades
                .Include(g => g.Evaluation)
                .Include(g => g.Course)
                .Where(g => usersToSearch.Contains(g.UserID) && g.SchoolID == schoolId)
                .Select(g => new
                {
                    g.GradeID,
                    g.UserID,
                    Estudiante = _context.Users.Where(u => u.UserID == g.UserID).Select(u => u.UserName).FirstOrDefault(),
                    Curso = g.Course.Name,
                    Evaluacion = g.Evaluation.Title,
                    g.GradeValue,
                    g.Comments
                })
                .ToListAsync();

            if (!grades.Any())
                return NotFound("No se encontraron calificaciones para los filtros especificados.");

            return Ok(grades);
        }

        [HttpGet("course/{courseId}/evaluations/all")]
        public async Task<IActionResult> GetEvaluationsByCourseAll(
    int courseId,
    [FromQuery] int? classroomId = null,
    [FromQuery] int? schoolId = null)
        {
            // Base query
            var query = _context.Evaluations
                .Where(e => e.CourseID == courseId);

            // Filtros opcionales
            if (classroomId.HasValue && classroomId > 0)
                query = query.Where(e => e.ClassroomID == classroomId);

            if (schoolId.HasValue && schoolId > 0)
                query = query.Where(e => e.SchoolID == schoolId);

            var evaluations = await query
                .Select(e => new
                {
                    e.EvaluationID,
                    e.Title,
                    e.Description,
                    e.CourseID,
                    e.ClassroomID,
                    e.SchoolID
                })
                .ToListAsync();

            if (!evaluations.Any())
                return NotFound("No hay evaluaciones para este curso.");

            return Ok(evaluations);
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