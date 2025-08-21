using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Obtener notificaciones de un usuario específico

        /* [HttpGet]
         public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int userID)
         {
             var notifications = await _context.Notifications
                 .Where(n => n.UserID == userID) // 🔹 Solo filtrar por UserID
                 .ToListAsync();

             return Ok(notifications);
         }*/

        //Con schoolid
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int userID, [FromQuery] int schoolID)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && n.SchoolID == schoolID)
                .ToListAsync();

            return Ok(notifications);
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
        /* [HttpPost]
         public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
         {
             _context.Notifications.Add(notification);
             await _context.SaveChangesAsync();
             return CreatedAtAction(nameof(GetNotification), new { id = notification.NotifyID }, notification);
         }*/

        //Con schoolid
        [HttpPost]
        public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
        {
            if (notification == null || notification.SchoolID == 0)
                return BadRequest("Falta SchoolID en la notificación.");

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetNotification), new { id = notification.NotifyID }, notification);
        }


        /* [HttpPost("batch")]
         public async Task<IActionResult> CreateNotifications([FromBody] List<Notification> notifications)
         {
             if (notifications == null || notifications.Count == 0)
                 return BadRequest("No hay notificaciones para registrar.");

             _context.Notifications.AddRange(notifications);
             await _context.SaveChangesAsync();

             return Ok(new { message = "Notificaciones creadas exitosamente.", count = notifications.Count });
         }*/

        //Con schoolid
        [HttpPost("batch")]
        public async Task<IActionResult> CreateNotifications([FromBody] List<Notification> notifications)
        {
            if (notifications == null || notifications.Count == 0)
                return BadRequest("No hay notificaciones para registrar.");

            if (notifications.Any(n => n.SchoolID == 0))
                return BadRequest("Todas las notificaciones deben tener un SchoolID válido.");

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
        /* [HttpPost("send-to-role")]
         public async Task<IActionResult> SendNotificationToRole([FromQuery] int roleId, [FromBody] Notification notification)
         {
             var users = await _context.Users
                 .Where(u => u.RoleID == roleId)
                 .ToListAsync();

             if (users == null || !users.Any())
                 return NotFound("No se encontraron usuarios con ese rol.");

             var notifications = users.Select(user => new Notification
             {
                 Title = notification.Title,
                 Content = notification.Content,
                 Date = DateTime.Now,
                 IsRead = false,
                 UserID = user.UserID
             }).ToList();

             _context.Notifications.AddRange(notifications);
             await _context.SaveChangesAsync();

             return Ok(new { message = "Notificaciones enviadas correctamente", count = notifications.Count });
         }*/

        //Con schoolid
        [HttpPost("send-to-role")]
        public async Task<IActionResult> SendNotificationToRole([FromQuery] int roleId, [FromQuery] int schoolId, [FromBody] Notification notification)
        {
            var users = await _context.Users
                .Where(u => u.RoleID == roleId && u.SchoolID == schoolId)
                .ToListAsync();

            if (!users.Any())
                return NotFound("No se encontraron usuarios con ese rol en esta escuela.");

            var notifications = users.Select(user => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = DateTime.Now,
                IsRead = false,
                UserID = user.UserID,
                SchoolID = schoolId
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones enviadas correctamente", count = notifications.Count });
        }


        //Enviar a todos
        /* [HttpPost("send-to-all")]
         public async Task<IActionResult> SendNotificationToAll([FromBody] Notification notification)
         {
             var users = await _context.Users.ToListAsync();

             var notifications = users.Select(user => new Notification
             {
                 Title = notification.Title,
                 Content = notification.Content,
                 Date = DateTime.Now,
                 IsRead = false,
                 UserID = user.UserID
             }).ToList();

             _context.Notifications.AddRange(notifications);
             await _context.SaveChangesAsync();

             return Ok(new { message = "Notificación enviada a todos los usuarios.", count = notifications.Count });
         }*/

        //Con schoolid
        [HttpPost("send-to-all")]
        public async Task<IActionResult> SendNotificationToAll([FromQuery] int schoolId, [FromBody] Notification notification)
        {
            var users = await _context.Users
                .Where(u => u.SchoolID == schoolId)
                .ToListAsync();

            if (!users.Any())
                return NotFound("No hay usuarios en esta escuela.");

            var notifications = users.Select(user => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = DateTime.Now,
                IsRead = false,
                UserID = user.UserID,
                SchoolID = schoolId
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación enviada a todos los usuarios de esta escuela.", count = notifications.Count });
        }


        //Enviar a padres de un salón específico
        [HttpPost("send-to-class")]
        public async Task<IActionResult> SendNotificationToClass([FromQuery] int classroomId, [FromQuery] int schoolId, [FromBody] Notification notification)
        {
            if (notification == null || string.IsNullOrEmpty(notification.Title) || string.IsNullOrEmpty(notification.Content))
            {
                return BadRequest("La notificación debe tener un título y un contenido.");
            }

            // 1. Obtener los IDs de usuario de los estudiantes en el salón específico.
            // Esto se hace a través de los "Enrollments" que están relacionados con los cursos del salón.
            var studentUserIDs = await _context.Enrollments
                .Where(e => e.Course.ClassroomID == classroomId && e.User.SchoolID == schoolId && e.User.RoleID == 1) // 1 es el RoleID para estudiantes
                .Select(e => e.UserID)
                .ToListAsync();

            if (!studentUserIDs.Any())
            {
                return NotFound("No se encontraron estudiantes en el salón especificado.");
            }

            // 2. Obtener los IDs únicos de los padres de esos estudiantes usando la tabla de relaciones.
            var parentIDs = await _context.UserRelationships
                .Where(ur => studentUserIDs.Contains(ur.User1ID) &&
                             ur.RelationshipType == "Padre-Hijo" &&
                             ur.SchoolID == schoolId)
                .Select(ur => ur.User2ID)
                .Distinct() // Esto asegura que un mismo padre no reciba notificaciones duplicadas
                .ToListAsync();

            if (!parentIDs.Any())
            {
                return NotFound("No se encontraron padres asociados a los estudiantes en este salón.");
            }

            // 3. Crear y preparar las notificaciones para cada padre
            var notificationsToSend = parentIDs.Select(parentID => new Notification
            {
                Title = notification.Title,
                Content = notification.Content,
                Date = DateTime.Now,
                IsRead = false,
                UserID = parentID,
                SchoolID = schoolId
            }).ToList();

            _context.Notifications.AddRange(notificationsToSend);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones enviadas correctamente a los padres del salón.", count = notificationsToSend.Count });
        }
    }
}



