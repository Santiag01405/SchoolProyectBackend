using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace SchoolProyectBackend.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        private DateTime GetVenezuelanTime()
        {
            var venezuelaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Venezuela Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, venezuelaTimeZone);
        }
        // POST: api/attendance/mark
        // Mantiene: notifica a padres sin filtrar por sede del padre.
        // Etiqueta cada notificación con la sede del alumno (attendance.SchoolID).
        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] List<Attendance> attendanceList)
        {
            if (attendanceList == null || attendanceList.Count == 0)
                return BadRequest("No hay datos de asistencia.");

            var venezuelanTime = GetVenezuelanTime();

            foreach (var attendance in attendanceList)
            {
                var user = await _context.Users.FindAsync(attendance.UserID);
                var course = await _context.Courses.FindAsync(attendance.CourseID);

                if (user == null || course == null || user.SchoolID != attendance.SchoolID || course.SchoolID != attendance.SchoolID)
                    return BadRequest("Usuario o curso no pertenecen al colegio indicado.");

                attendance.Date = venezuelanTime;
            }

            _context.Attendance.AddRange(attendanceList);
            await _context.SaveChangesAsync();

            var notifications = new List<Notification>();

            foreach (var attendance in attendanceList)
            {
                if (attendance.Status == "Ausente")
                {
                    var parents = await _context.UserRelationships
                        .Where(ur => ur.User1ID == attendance.UserID && ur.RelationshipType == "Padre-Hijo") // 👈 sin filtrar por sede del padre
                        .Select(ur => ur.User2ID)
                        .ToListAsync();

                    // (opcional) incluir nombre del estudiante para mejor UX
                    var studentName = await _context.Users
                        .Where(u => u.UserID == attendance.UserID)
                        .Select(u => u.UserName)
                        .FirstOrDefaultAsync();

                    foreach (var parentId in parents)
                    {
                        notifications.Add(new Notification
                        {
                            UserID = parentId,
                            Title = "Notificación de Asistencia",
                            Content = $"Tu hijo/a {studentName} ha sido marcado como {attendance.Status} en la clase.",
                            Date = venezuelanTime,
                            SchoolID = attendance.SchoolID // 👈 sede del alumno
                        });
                    }
                }
            }

            if (notifications.Count > 0)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.PostAsJsonAsync(
                        "https://SchoolProject123.somee.com//api/notifications/batch",
                        notifications
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error al enviar notificaciones: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Excepción al intentar enviar notificaciones: {ex.Message}");
                }
            }

            return Ok(new { message = "Asistencia registrada y notificaciones enviadas." });
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetAttendanceByCourse(int courseId, [FromQuery] int schoolId)
        {
            var attendanceRecords = await _context.Attendance
                .Where(a => a.CourseID == courseId && a.SchoolID == schoolId)
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserID,
                    StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                    a.RelatedUserID,
                    TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                    a.CourseID,
                    CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                    a.Date,
                    a.Status
                })
                .ToListAsync();

            return Ok(attendanceRecords);
        }

        // GET: api/attendance/parent/{userId}[?schoolId=5]
        // Si no envías schoolId (o es <= 0), devuelve asistencias de TODOS los hijos del padre en TODAS las sedes.
        // Si envías schoolId > 0, filtra por esa sede.
        [HttpGet("parent/{userId}")]
        public async Task<IActionResult> GetAttendanceByParent(int userId, [FromQuery] int? schoolId = null)
        {
            // Hijos del padre (de cualquier sede)
            var studentIds = await _context.UserRelationships
                .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User1ID)
                .ToListAsync();

            if (!studentIds.Any())
                return Ok(new List<object>());

            var q = _context.Attendance.AsQueryable()
                .Where(a => studentIds.Contains(a.UserID));

            if (schoolId.HasValue && schoolId.Value > 0)
                q = q.Where(a => a.SchoolID == schoolId.Value);

            var attendanceRecords = await q
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserID,
                    StudentName = _context.Users.Where(u => u.UserID == a.UserID).Select(u => u.UserName).FirstOrDefault(),
                    a.RelatedUserID,
                    TeacherName = _context.Users.Where(u => u.UserID == a.RelatedUserID).Select(u => u.UserName).FirstOrDefault(),
                    a.CourseID,
                    CourseName = _context.Courses.Where(c => c.CourseID == a.CourseID).Select(c => c.Name).FirstOrDefault(),
                    a.Date,
                    a.Status,
                    a.SchoolID // 👈 importante: la sede de la asistencia
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return Ok(attendanceRecords);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id, [FromQuery] int schoolId)
        {
            var attendance = await _context.Attendance.FindAsync(id);

            if (attendance == null || attendance.SchoolID != schoolId)
                return NotFound(new { message = "Asistencia no encontrada para esta escuela." });

            _context.Attendance.Remove(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Asistencia eliminada correctamente." });
        }




        // Estadísticas de asistencia


        [HttpGet("stats/student/{studentId}")]
        public async Task<IActionResult> GetStudentStats(
    int studentId,
    [FromQuery] int? schoolId = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
        {
            // Nombre del alumno
            var student = await _context.Users.AsNoTracking()
                .Where(u => u.UserID == studentId)
                .Select(u => new { u.UserID, u.UserName })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound("Alumno no encontrado.");

            // Base query de asistencias de ese alumno
            var baseQ = _context.Attendance.AsNoTracking()
                .Where(a => a.UserID == studentId);

            baseQ = ApplyRangeFilters(baseQ, from, to, schoolId);

            // Resumen global
            var overallAgg = await baseQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .FirstOrDefaultAsync() ?? new { Total = 0, Present = 0, Absent = 0 };

            // Desglose por curso
            var byCourse = await baseQ
                .GroupBy(a => new { a.CourseID })
                .Select(g => new
                {
                    g.Key.CourseID,
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .ToListAsync();

            // Traer nombres de cursos
            var courseIds = byCourse.Select(x => x.CourseID).ToList();
            var courseNames = await _context.Courses.AsNoTracking()
                .Where(c => courseIds.Contains(c.CourseID))
                .Select(c => new { c.CourseID, c.Name })
                .ToDictionaryAsync(x => x.CourseID, x => x.Name);

            var resp = new StudentStatsResponse
            {
                StudentID = student.UserID,
                StudentName = student.UserName,
                Overall = new AttendanceSummaryDto
                {
                    Total = overallAgg.Total,
                    Present = overallAgg.Present,
                    Absent = overallAgg.Absent
                },
                ByCourse = byCourse.Select(x => new StudentCourseStatDto
                {
                    CourseID = x.CourseID,
                    CourseName = courseNames.TryGetValue(x.CourseID, out var nm) ? nm : "",
                    Summary = new AttendanceSummaryDto
                    {
                        Total = x.Total,
                        Present = x.Present,
                        Absent = x.Absent
                    }
                })
                .OrderBy(c => c.CourseName)
                .ToList()
            };

            return Ok(resp);
        }
        private IQueryable<Attendance> ApplyRangeFilters(
            IQueryable<Attendance> q,
            DateTime? from, DateTime? to, int? schoolId)
        {
            if (from.HasValue) q = q.Where(a => a.Date >= from.Value);
            if (to.HasValue) q = q.Where(a => a.Date < to.Value);
            if (schoolId.HasValue && schoolId.Value > 0)
                q = q.Where(a => a.SchoolID == schoolId.Value);
            return q;
        }



        [HttpGet("stats/student/{studentId}/course/{courseId}")]
        public async Task<IActionResult> GetStudentCourseStats(
    int studentId, int courseId,
    [FromQuery] int? schoolId = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
        {
            var stu = await _context.Users.AsNoTracking()
                .Where(u => u.UserID == studentId)
                .Select(u => new { u.UserID, u.UserName })
                .FirstOrDefaultAsync();
            if (stu == null) return NotFound("Alumno no encontrado.");

            var course = await _context.Courses.AsNoTracking()
                .Where(c => c.CourseID == courseId)
                .Select(c => new { c.CourseID, c.Name })
                .FirstOrDefaultAsync();
            if (course == null) return NotFound("Curso no encontrado.");

            var q = _context.Attendance.AsNoTracking()
                .Where(a => a.UserID == studentId && a.CourseID == courseId);
            q = ApplyRangeFilters(q, from, to, schoolId);

            var agg = await q.GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .FirstOrDefaultAsync() ?? new { Total = 0, Present = 0, Absent = 0 };

            var resp = new StudentCourseSingleResponse
            {
                StudentID = stu.UserID,
                StudentName = stu.UserName,
                CourseID = course.CourseID,
                CourseName = course.Name,
                Summary = new AttendanceSummaryDto
                {
                    Total = agg.Total,
                    Present = agg.Present,
                    Absent = agg.Absent
                }
            };

            return Ok(resp);
        }

        [HttpGet("stats/classroom/{classroomId}")]
        public async Task<IActionResult> GetClassroomStats(
    int classroomId,
    [FromQuery] int? schoolId = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
        {
            // Info del classroom
            var classroom = await _context.Classrooms.AsNoTracking()
                .Where(c => c.ClassroomID == classroomId)
                .Select(c => new { c.ClassroomID, c.Name })
                .FirstOrDefaultAsync();

            if (classroom == null)
                return NotFound("Salón no encontrado.");

            // Estudiantes del salón
            var studentIds = await _context.Users.AsNoTracking()
                .Where(u => u.RoleID == 1 && u.ClassroomID == classroomId)
                .Select(u => new { u.UserID, u.UserName })
                .ToListAsync();

            if (studentIds.Count == 0)
                return Ok(new ClassroomStatsResponse
                {
                    ClassroomID = classroom.ClassroomID,
                    ClassroomName = classroom.Name
                });

            var sidList = studentIds.Select(x => x.UserID).ToList();

            // Base query: asistencias de estudiantes del salón
            var baseQ = _context.Attendance.AsNoTracking()
                .Where(a => sidList.Contains(a.UserID));

            baseQ = ApplyRangeFilters(baseQ, from, to, schoolId);

            // Overall salón
            var overall = await baseQ.GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .FirstOrDefaultAsync() ?? new { Total = 0, Present = 0, Absent = 0 };

            // Por curso (de todos los estudiantes del salón)
            var byCourse = await baseQ
                .GroupBy(a => a.CourseID)
                .Select(g => new
                {
                    CourseID = g.Key,
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .ToListAsync();

            var courseIds = byCourse.Select(x => x.CourseID).ToList();
            var courseNames = await _context.Courses.AsNoTracking()
                .Where(c => courseIds.Contains(c.CourseID))
                .Select(c => new { c.CourseID, c.Name })
                .ToDictionaryAsync(x => x.CourseID, x => x.Name);

            // Por estudiante (sumado todas sus asistencias dentro del rango)
            var byStudent = await baseQ
                .GroupBy(a => a.UserID)
                .Select(g => new
                {
                    StudentID = g.Key,
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Presente"),
                    Absent = g.Count(x => x.Status == "Ausente")
                })
                .ToListAsync();

            var nameById = studentIds.ToDictionary(x => x.UserID, x => x.UserName);

            var resp = new ClassroomStatsResponse
            {
                ClassroomID = classroom.ClassroomID,
                ClassroomName = classroom.Name,
                Overall = new AttendanceSummaryDto
                {
                    Total = overall.Total,
                    Present = overall.Present,
                    Absent = overall.Absent
                },
                ByCourse = byCourse.Select(x => new CourseSummaryDto
                {
                    CourseID = x.CourseID,
                    CourseName = courseNames.TryGetValue(x.CourseID, out var nm) ? nm : "",
                    Summary = new AttendanceSummaryDto
                    {
                        Total = x.Total,
                        Present = x.Present,
                        Absent = x.Absent
                    }
                })
                .OrderBy(c => c.CourseName)
                .ToList(),
                ByStudent = byStudent.Select(x => new ClassroomStudentStatDto
                {
                    StudentID = x.StudentID,
                    StudentName = nameById.TryGetValue(x.StudentID, out var nm) ? nm : "",
                    Summary = new AttendanceSummaryDto
                    {
                        Total = x.Total,
                        Present = x.Present,
                        Absent = x.Absent
                    }
                })
                .OrderBy(s => s.StudentName)
                .ToList()
            };

            return Ok(resp);
        }

        private async Task<(DateTime from, DateTime to)> GetLapsoRangeAsync(int lapsoId)
        {
            var lapso = await _context.Lapsos
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LapsoID == lapsoId);

            if (lapso == null)
                throw new KeyNotFoundException("Lapso no encontrado.");

            // Usamos [from, to) (incluye inicio, excluye fin)
            var from = lapso.FechaInicio.Date;
            var to = lapso.FechaFin.Date.AddDays(1);

            return (from, to);
        }

        [HttpGet("stats/student/{studentId}/lapso/{lapsoId}")]
        public async Task<IActionResult> GetStudentStatsByLapso(
    int studentId,
    int lapsoId,
    [FromQuery] int? schoolId = null)
        {
            try
            {
                var (from, to) = await GetLapsoRangeAsync(lapsoId);
                // Reutiliza tu endpoint por rango:
                return await GetStudentStats(studentId, schoolId, from, to);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("stats/student/{studentId}/course/{courseId}/lapso/{lapsoId}")]
        public async Task<IActionResult> GetStudentCourseStatsByLapso(
    int studentId,
    int courseId,
    int lapsoId,
    [FromQuery] int? schoolId = null)
        {
            try
            {
                var (from, to) = await GetLapsoRangeAsync(lapsoId);
                return await GetStudentCourseStats(studentId, courseId, schoolId, from, to);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("stats/classroom/{classroomId}/lapso/{lapsoId}")]
        public async Task<IActionResult> GetClassroomStatsByLapso(
    int classroomId,
    int lapsoId,
    [FromQuery] int? schoolId = null)
        {
            try
            {
                var (from, to) = await GetLapsoRangeAsync(lapsoId);
                return await GetClassroomStats(classroomId, schoolId, from, to);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("stats/school/{schoolId}/lapso/{lapsoId}")]
        public async Task<IActionResult> GetSchoolStatsByLapso(
    int schoolId,
    int lapsoId)
        {
            try
            {
                var (from, to) = await GetLapsoRangeAsync(lapsoId);

                // Total por escuela en el lapso
                var q = _context.Attendance.AsNoTracking()
                    .Where(a => a.SchoolID == schoolId && a.Date >= from && a.Date < to);

                var total = await q.CountAsync();
                var presentes = await q.CountAsync(a => a.Status == "Presente");
                var ausentes = await q.CountAsync(a => a.Status == "Ausente");

                // Por curso (si quieres desglose)
                var porCurso = await q
                    .GroupBy(a => a.CourseID)
                    .Select(g => new
                    {
                        CourseID = g.Key,
                        Total = g.Count(),
                        Presentes = g.Count(a => a.Status == "Presente"),
                        Ausentes = g.Count(a => a.Status == "Ausente")
                    })
                    .OrderBy(x => x.CourseID)
                    .ToListAsync();

                return Ok(new
                {
                    From = from,
                    To = to.AddDays(-1), // visualmente “hasta”, inclusive
                    SchoolID = schoolId,
                    Total = total,
                    Presentes = presentes,
                    Ausentes = ausentes,
                    PorCurso = porCurso
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // GET: api/attendance/stats/school/{schoolId}/by-lapsos[?year=2025]
        // Devuelve, por cada lapso de esa escuela, los totales (Presente/Ausente) y desglose por curso y por salón.
        [HttpGet("stats/school/{schoolId}/by-lapsos")]
        public async Task<IActionResult> GetSchoolStatsByLapsos(int schoolId, [FromQuery] int? year = null)
        {
            // 1) Trae los lapsos de esa escuela (opcional: filtra por año si lo piden)
            var lapsosQ = _context.Lapsos
                .AsNoTracking()
                .Where(l => l.SchoolID == schoolId);

            if (year.HasValue && year.Value > 0)
            {
                // Incluye lapsos que toquen ese año (cualquier solapamiento con el calendario del año)
                var from = new DateTime(year.Value, 1, 1);
                var to = from.AddYears(1);
                lapsosQ = lapsosQ.Where(l => l.FechaFin >= from && l.FechaInicio < to);
            }

            var lapsos = await lapsosQ
                .OrderBy(l => l.FechaInicio)
                .ToListAsync();

            if (!lapsos.Any())
                return Ok(new { SchoolID = schoolId, Lapsos = new List<object>() });

            var resultados = new List<object>();

            foreach (var lapso in lapsos)
            {
                // 2) Rango del lapso
                var ini = lapso.FechaInicio;
                var fin = lapso.FechaFin;

                // 3) Query base de asistencias del lapso (de esa escuela)
                var q = _context.Attendance.AsNoTracking()
                    .Where(a => a.SchoolID == schoolId &&
                                a.Date >= ini && a.Date <= fin);

                // Totales del lapso
                var total = await q.CountAsync();
                var presentes = await q.CountAsync(a => a.Status == "Presente");
                var ausentes = await q.CountAsync(a => a.Status == "Ausente");

                // Desglose por curso dentro del lapso
                var porCurso = await q
                    .GroupBy(a => a.CourseID)
                    .Select(g => new
                    {
                        CourseID = g.Key,
                        Total = g.Count(),
                        Presentes = g.Count(x => x.Status == "Presente"),
                        Ausentes = g.Count(x => x.Status == "Ausente")
                    })
                    .OrderBy(x => x.CourseID)
                    .ToListAsync();

                // Desglose por salón (si el estudiante tiene ClassroomID)
                var studentWithClassroom = from a in q
                                           join u in _context.Users.AsNoTracking()
                                               on a.UserID equals u.UserID
                                           select new { a.Status, u.ClassroomID };

                var porSalon = await studentWithClassroom
                    .Where(x => x.ClassroomID != null)
                    .GroupBy(x => x.ClassroomID)
                    .Select(g => new
                    {
                        ClassroomID = g.Key,
                        Total = g.Count(),
                        Presentes = g.Count(x => x.Status == "Presente"),
                        Ausentes = g.Count(x => x.Status == "Ausente")
                    })
                    .OrderBy(x => x.ClassroomID)
                    .ToListAsync();

                resultados.Add(new
                {
                    LapsoID = lapso.LapsoID,
                    Nombre = (lapso.Nombre ?? $"Lapso {lapso.LapsoID}"), // si tienes campo Nombre; si no, quita esta línea
                    FechaInicio = ini,
                    FechaFin = fin,
                    Total = total,
                    Presentes = presentes,
                    Ausentes = ausentes,
                    PorCurso = porCurso,
                    PorSalon = porSalon
                });
            }

            return Ok(new
            {
                SchoolID = schoolId,
                YearFilter = year, // null si no filtraste por año
                Lapsos = resultados
            });
        }

        public class AttendanceSummaryDto
        {
            public int Total { get; set; }
            public int Present { get; set; }
            public int Absent { get; set; }
            public double AttendanceRate => Total == 0 ? 0 : Math.Round((double)Present * 100.0 / Total, 2);
        }

        public class StudentCourseStatDto
        {
            public int CourseID { get; set; }
            public string CourseName { get; set; } = "";
            public AttendanceSummaryDto Summary { get; set; } = new();
        }

        public class StudentStatsResponse
        {
            public int StudentID { get; set; }
            public string StudentName { get; set; } = "";
            public AttendanceSummaryDto Overall { get; set; } = new();
            public List<StudentCourseStatDto> ByCourse { get; set; } = new();
        }

        public class StudentCourseSingleResponse
        {
            public int StudentID { get; set; }
            public string StudentName { get; set; } = "";
            public int CourseID { get; set; }
            public string CourseName { get; set; } = "";
            public AttendanceSummaryDto Summary { get; set; } = new();
        }

        public class CourseSummaryDto
        {
            public int CourseID { get; set; }
            public string CourseName { get; set; } = "";
            public AttendanceSummaryDto Summary { get; set; } = new();
        }

        public class ClassroomStudentStatDto
        {
            public int StudentID { get; set; }
            public string StudentName { get; set; } = "";
            public AttendanceSummaryDto Summary { get; set; } = new();
        }

        public class ClassroomStatsResponse
        {
            public int ClassroomID { get; set; }
            public string ClassroomName { get; set; } = "";
            public AttendanceSummaryDto Overall { get; set; } = new();
            public List<CourseSummaryDto> ByCourse { get; set; } = new();
            public List<ClassroomStudentStatDto> ByStudent { get; set; } = new();
        }
    }

}

