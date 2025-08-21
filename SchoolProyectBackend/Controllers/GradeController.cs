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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == grade.UserID);
            var evaluation = await _context.Evaluations.FirstOrDefaultAsync(e => e.EvaluationID == grade.EvaluationID);

            if (user == null || evaluation == null)
                return NotFound("Usuario o evaluación no encontrada.");

            if (grade.CourseID == 0)
            {
                grade.CourseID = evaluation.CourseID;
            }

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
                await _context.SaveChangesAsync();

                // Lógica para notificar a los padres usando la misma estructura que asistencias
                await NotifyParents(grade, user.UserName, evaluation.Title);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Calificación registrada correctamente." });
        }

        private async Task NotifyParents(Grade grade, string studentName, string evaluationTitle)
        {
            var parents = await _context.UserRelationships
                .Where(ur => ur.User1ID == grade.UserID && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User2ID)
                .ToListAsync();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == grade.CourseID);

            if (parents.Any() && course != null)
            {
                foreach (var parentId in parents)
                {
                    var notification = new Notification
                    {
                        UserID = parentId,
                        Title = "Nueva Calificación",
                        Content = $"Se ha subido una nueva calificación para {studentName} en la evaluación '{evaluationTitle}' del curso '{course.Name}'. Nota: {grade.GradeValue}",
                        IsRead = false,
                        Date = DateTime.Now,
                        SchoolID = grade.SchoolID
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();
            }
        }

        // El endpoint para obtener notificaciones genéricas
        [HttpGet("notifications/user/{userId}")]
        public async Task<IActionResult> GetNotificationsForUser(int userId, [FromQuery] int schoolId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.SchoolID == schoolId);
            if (user == null)
            {
                return NotFound("Usuario no encontrado en esta escuela.");
            }

            // Aquí simplemente filtramos por el usuario y el colegio, sin referencia a GradeID
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId && n.SchoolID == schoolId)
                .OrderByDescending(n => n.Date)
                .ToListAsync();

            if (!notifications.Any())
            {
                return NotFound("No se encontraron notificaciones para este usuario.");
            }

            return Ok(notifications);
        }


        /*   [HttpGet("user/{userId}/grades")]
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
           }*/

        [HttpGet("student/{studentId}/lapso/{lapsoId}")]
        public async Task<IActionResult> GetGradesByLapso(int studentId, int lapsoId, [FromQuery] int schoolId)
        {
            // 1. Verificar si el estudiante existe y pertenece a la escuela
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == studentId && u.SchoolID == schoolId);

            if (student == null)
            {
                return NotFound("Estudiante no encontrado en este colegio.");
            }

            // 2. Obtener las notas del estudiante para el lapso especificado
            var grades = await _context.Grades
                .Where(g => g.UserID == studentId && g.SchoolID == schoolId && g.Evaluation.LapsoID == lapsoId)
                .Include(g => g.Evaluation) // Incluir los datos de la evaluación
                    .ThenInclude(e => e.Course) // Y los datos del curso de esa evaluación
                .ToListAsync();

            if (!grades.Any())
            {
                return NotFound("No se encontraron calificaciones para este estudiante en el lapso y colegio especificados.");
            }

            return Ok(grades);
        }


        [HttpGet("user/{userId}/grades")]
        public async Task<IActionResult> GetGradesForUser(int userId, [FromQuery] int schoolId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.SchoolID == schoolId);
            if (user == null)
                return NotFound("Usuario no encontrado en esta escuela.");

            List<int> usersToSearch = new();
            if (user.RoleID == 1) // Estudiante
            {
                usersToSearch.Add(userId);
            }
            else
            {
                var children = await _context.UserRelationships
                    .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo" && ur.SchoolID == schoolId)
                    .Select(ur => ur.User1ID)
                    .ToListAsync();
                if (!children.Any())
                    return NotFound("No se encontraron hijos asociados a este usuario en esta escuela.");
                usersToSearch.AddRange(children);
            }

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
        // 🟢 NUEVO ENDPOINT: Promedio de un Estudiante por Lapso
        // GET: api/grades/student/{userId}/average-by-lapso?schoolId=1&lapsoId=1
        [HttpGet("student/{userId}/average-by-lapso")]
        public async Task<IActionResult> GetStudentAverageByLapso(int userId, [FromQuery] int schoolId, [FromQuery] int lapsoId)
        {
            if (userId == 0 || schoolId == 0 || lapsoId == 0)
            {
                return BadRequest("El ID del usuario, la escuela y el lapso son obligatorios.");
            }

            var studentProfile = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.SchoolID == schoolId && u.RoleID == 1);

            if (studentProfile == null)
            {
                return NotFound("Estudiante no encontrado en esta escuela.");
            }

            // ✅ Unimos con la tabla de Evaluaciones para poder filtrar por LapsoID.
            var average = await _context.Grades
                .Include(g => g.Evaluation)
                .Where(g => g.UserID == userId && g.SchoolID == schoolId && g.Evaluation.LapsoID == lapsoId)
                .Select(g => g.GradeValue)
                .Where(g => g.HasValue)
                .AverageAsync();

            var result = new
            {
                StudentId = studentProfile.UserID,
                StudentName = studentProfile.UserName,
                AverageGrade = average
            };

            return Ok(result);
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


        // Promedio General de un Estudiante
        [HttpGet("student/{userId}/overall-average")]
        public async Task<IActionResult> GetStudentOverallAverage(int userId, [FromQuery] int schoolId)
        {
            if (userId == 0 || schoolId == 0)
            {
                return BadRequest("El ID del usuario y de la escuela son obligatorios.");
            }

            var studentProfile = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.SchoolID == schoolId && u.RoleID == 1);

            if (studentProfile == null)
            {
                return NotFound("Estudiante no encontrado en esta escuela.");
            }

            var overallAverage = await _context.Grades
                .Where(g => g.UserID == userId && g.SchoolID == schoolId)
                .Select(g => g.GradeValue)
                .Where(g => g.HasValue) // Solo promedia notas que tienen un valor
                .AverageAsync();

            var result = new
            {
                StudentId = studentProfile.UserID,
                StudentName = studentProfile.UserName,
                OverallAverage = overallAverage
            };

            return Ok(result);
        }



        [HttpGet("student/{studentId}/course/{courseId}/average")]
        public async Task<IActionResult> GetStudentCourseAverage(int studentId, int courseId, [FromQuery] int schoolId)
        {
            if (studentId == 0 || courseId == 0 || schoolId == 0)
            {
                return BadRequest("El ID del estudiante, curso y escuela son obligatorios.");
            }

            var studentCourseGrades = await _context.Grades
                .Where(g => g.UserID == studentId && g.CourseID == courseId && g.SchoolID == schoolId)
                .Select(g => g.GradeValue)
                .Where(g => g.HasValue) // Solo promedia notas que tienen un valor asignado
                .ToListAsync();

            if (!studentCourseGrades.Any())
            {
                return NotFound("No se encontraron calificaciones para este estudiante en el curso especificado.");
            }

            var average = studentCourseGrades.Average();
            var result = new
            {
                StudentId = studentId,
                CourseId = courseId,
                AverageGrade = average
            };

            return Ok(result);
        }


        // Promedio de un Curso (de todos los estudiantes)
        [HttpGet("course/{courseId}/average")]
        public async Task<IActionResult> GetCourseAverage(int courseId, [FromQuery] int schoolId)
        {
            if (courseId == 0 || schoolId == 0)
            {
                return BadRequest("El ID del curso y de la escuela son obligatorios.");
            }

            var courseAverage = await _context.Grades
                .Where(g => g.CourseID == courseId && g.SchoolID == schoolId)
                .Select(g => g.GradeValue)
                .Where(g => g.HasValue) // Solo promedia notas que tienen un valor
                .AverageAsync();

            var result = new
            {
                CourseId = courseId,
                AverageGrade = courseAverage
            };

            return Ok(result);
        }


        [HttpPost("assign-evaluation")]
        public async Task<IActionResult> AssignEvaluationToStudent([FromBody] Grade grade)
        {
            // Valida los datos mínimos necesarios
            if (grade.UserID == 0 || grade.EvaluationID == 0 || grade.SchoolID == 0)
                return BadRequest("Datos incompletos.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == grade.UserID);
            var evaluation = await _context.Evaluations.FirstOrDefaultAsync(e => e.EvaluationID == grade.EvaluationID);

            if (user == null || evaluation == null)
                return NotFound("Usuario o evaluación no encontrada.");

            // Verifica que la evaluación no haya sido asignada previamente a este estudiante
            var existingAssignment = await _context.Grades
                .FirstOrDefaultAsync(g => g.UserID == grade.UserID && g.EvaluationID == grade.EvaluationID);

            if (existingAssignment != null)
            {
                return BadRequest("Esta evaluación ya ha sido asignada a este estudiante.");
            }

            // ✅ Crea la asignación con un valor de calificación nulo
            var newGradeAssignment = new Grade
            {
                UserID = grade.UserID,
                EvaluationID = grade.EvaluationID,
                CourseID = evaluation.CourseID,
                SchoolID = grade.SchoolID,
                GradeValue = null,
                Comments = "Evaluación asignada."
            };

            _context.Grades.Add(newGradeAssignment);
            await _context.SaveChangesAsync();

            // 🚀 Notifica a los padres de este único estudiante
            await NotifyParentsForNewAssignment(newGradeAssignment, user.UserName, evaluation.Title);

            return Ok(new { message = "Evaluación asignada al estudiante correctamente." });
        }

        private async Task NotifyParentsForNewAssignment(Grade grade, string studentName, string evaluationTitle)
        {
            var parents = await _context.UserRelationships
                .Where(ur => ur.User1ID == grade.UserID && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User2ID)
                .ToListAsync();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == grade.CourseID);

            if (parents.Any() && course != null)
            {
                foreach (var parentId in parents)
                {
                    var notification = new Notification
                    {
                        UserID = parentId,
                        Title = "Nueva Evaluación Asignada",
                        Content = $"Se ha asignado una nueva evaluación: '{evaluationTitle}' para tu hijo/a **{studentName}** en el curso de {course.Name}.",
                        IsRead = false,
                        Date = DateTime.Now,
                        SchoolID = grade.SchoolID
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEvaluationsByStudent(int studentId, [FromQuery] int? lapsoId)
        {
            var query = _context.Evaluations.Where(e => e.UserID == studentId);

            if (lapsoId.HasValue)
            {
                query = query.Where(e => e.LapsoID == lapsoId.Value);
            }

            var evaluaciones = await query.ToListAsync();
            return Ok(evaluaciones);
        }
    }
}