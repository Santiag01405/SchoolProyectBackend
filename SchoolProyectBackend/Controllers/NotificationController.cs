using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private DateTime GetVenezuelanTime()
        {
            var venezuelaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Venezuela Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, venezuelaTimeZone);
        }

        // Obtener notificaciones de un usuario específico
        // GET: api/notifications?userID=73[&schoolID=5]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications(
            [FromQuery] int userID,
            [FromQuery] int? schoolID = null)
        {
            if (userID <= 0) return BadRequest("userID inválido.");

            IQueryable<Notification> q = _context.Notifications.Where(n => n.UserID == userID);

            if (schoolID.HasValue && schoolID.Value > 0)
                q = q.Where(n => n.SchoolID == schoolID.Value);

            var list = await q.OrderByDescending(n => n.Date).ToListAsync();
            return Ok(list);
        }


        // Obtener notificación por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            return Ok(notification);
        }

        // Crear una nueva notificación
        [HttpPost]
        public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
        {
            if (notification == null || notification.SchoolID == 0)
                return BadRequest("Falta SchoolID en la notificación.");

            notification.Date = GetVenezuelanTime(); // ⬅️ Cambio aquí

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetNotification), new { id = notification.NotifyID }, notification);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateNotifications([FromBody] List<Notification> notifications)
        {
            if (notifications == null || notifications.Count == 0)
                return BadRequest("No hay notificaciones para registrar.");

            if (notifications.Any(n => n.SchoolID == 0))
                return BadRequest("Todas las notificaciones deben tener un SchoolID válido.");

            var venezuelaTime = GetVenezuelanTime(); // ⬅️ Cambio aquí

            foreach (var notification in notifications)
            {
                notification.Date = venezuelaTime; // ⬅️ Cambio aquí
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones creadas exitosamente.", count = notifications.Count });
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //Enviar a rol especifico
        // Enviar a un ROL específico EN LA SEDE + (si el rol es PADRES) incluir padres externos relacionados a alumnos de la sede
        [HttpPost("send-to-role")]
        public async Task<IActionResult> SendNotificationToRole(
            [FromQuery] int roleId,
            [FromQuery] int schoolId,
            [FromBody] Notification notification)
        {
            if (roleId <= 0 || schoolId <= 0) return BadRequest("roleId o schoolId inválido.");
            if (notification == null || string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Content))
                return BadRequest("La notificación debe tener título y contenido.");

            var venezuelaTime = GetVenezuelanTime();

            List<int> targetUserIds;

            if (roleId == 3) // PADRES
            {
                // Alumnos de la sede
                var studentIds = await _context.Users
                    .Where(u => u.RoleID == 1 && u.SchoolID == schoolId)
                    .Select(u => u.UserID)
                    .ToListAsync();

                if (!studentIds.Any())
                    return NotFound("No se encontraron estudiantes en esta escuela.");

                // Padres de esos alumnos (SIN filtrar por sede del padre)
                targetUserIds = await _context.UserRelationships
                    .Where(ur => studentIds.Contains(ur.User1ID) && ur.RelationshipType == "Padre-Hijo")
                    .Select(ur => ur.User2ID)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                // Para otros roles, se mantiene el comportamiento original (usuarios de ese rol en la sede)
                targetUserIds = await _context.Users
                    .Where(u => u.RoleID == roleId && u.SchoolID == schoolId)
                    .Select(u => u.UserID)
                    .ToListAsync();
            }

            if (!targetUserIds.Any())
                return NotFound("No se encontraron usuarios con ese rol (o padres) para esta escuela.");

            var notifications = targetUserIds.Select(uid => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = venezuelaTime,
                IsRead = false,
                UserID = uid,
                SchoolID = schoolId // Sede origen del aviso
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones enviadas correctamente.", count = notifications.Count });
        }



        //Enviar a todos
        // Enviar a todos (usuarios de la sede) + PADRES de alumnos de la sede (aunque los padres estén en otra sede)
        [HttpPost("send-to-all")]
        public async Task<IActionResult> SendNotificationToAll(
            [FromQuery] int schoolId,
            [FromBody] Notification notification)
        {
            if (schoolId <= 0) return BadRequest("schoolId inválido.");
            if (notification == null || string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Content))
                return BadRequest("La notificación debe tener título y contenido.");

            var venezuelaTime = GetVenezuelanTime();

            // 1) Todos los usuarios pertenecientes a esta sede (alumnos, docentes, admins…)
            var baseUserIds = await _context.Users
                .Where(u => u.SchoolID == schoolId)
                .Select(u => u.UserID)
                .ToListAsync();

            // 2) Alumnos de esta sede
            var studentIds = await _context.Users
                .Where(u => u.RoleID == 1 && u.SchoolID == schoolId)
                .Select(u => u.UserID)
                .ToListAsync();

            // 3) Padres relacionados con esos alumnos (SIN filtrar por sede del padre)
            var parentIds = await _context.UserRelationships
                .Where(ur => studentIds.Contains(ur.User1ID) && ur.RelationshipType == "Padre-Hijo")
                .Select(ur => ur.User2ID)
                .Distinct()
                .ToListAsync();

            // 4) Unión de destinatarios (usuarios de la sede + padres externos)
            var targetUserIds = baseUserIds
                .Concat(parentIds)
                .Distinct()
                .ToList();

            if (!targetUserIds.Any())
                return NotFound("No se encontraron destinatarios para esta escuela.");

            var notifications = targetUserIds.Select(uid => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = venezuelaTime,
                IsRead = false,
                UserID = uid,
                SchoolID = schoolId // Etiquetamos SIEMPRE con la sede origen del aviso
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación enviada a todos los usuarios y padres relacionados con alumnos de la sede.", count = notifications.Count });
        }



        //Enviar a padres de un salón específico
        [HttpPost("send-to-class")]
        public async Task<IActionResult> SendNotificationToClass([FromQuery] int classroomId, [FromQuery] int schoolId, [FromBody] Notification notification)
        {
            if (notification == null || string.IsNullOrEmpty(notification.Title) || string.IsNullOrEmpty(notification.Content))
            {
                return BadRequest("La notificación debe tener un título y un contenido.");
            }

            var studentUserIDs = await _context.Enrollments
                .Where(e => e.Course.ClassroomID == classroomId && e.User.SchoolID == schoolId && e.User.RoleID == 1)
                .Select(e => e.UserID)
                .ToListAsync();

            if (!studentUserIDs.Any())
            {
                return NotFound("No se encontraron estudiantes en el salón especificado.");
            }

            var parentIDs = await _context.UserRelationships
     .Where(ur => studentUserIDs.Contains(ur.User1ID) &&
                  ur.RelationshipType == "Padre-Hijo")     
     .Select(ur => ur.User2ID)
     .Distinct()
     .ToListAsync();


            if (!parentIDs.Any())
            {
                return NotFound("No se encontraron padres asociados a los estudiantes en este salón.");
            }

            var venezuelaTime = GetVenezuelanTime(); // ⬅️ Cambio aquí

            var notificationsToSend = parentIDs.Select(parentID => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = venezuelaTime, // ⬅️ Cambio aquí
                IsRead = false,
                UserID = parentID,
                SchoolID = schoolId
            }).ToList();

            _context.Notifications.AddRange(notificationsToSend);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones enviadas correctamente a los padres del salón.", count = notificationsToSend.Count });
        }

        // PUT: api/notifications/read-all?userID=73[&schoolID=5]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            [FromQuery] int userID,
            [FromQuery] int? schoolID = null)
        {
            if (userID <= 0) return BadRequest("userID inválido.");

            var q = _context.Notifications.Where(n => n.UserID == userID && !n.IsRead);

            if (schoolID.HasValue && schoolID.Value > 0)
                q = q.Where(n => n.SchoolID == schoolID.Value);

            var toUpdate = await q.ToListAsync();
            if (!toUpdate.Any())
                return Ok(new { message = "No hay notificaciones sin leer para este filtro." });

            foreach (var n in toUpdate) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Se marcaron {toUpdate.Count} como leídas." });
        }

    }
}