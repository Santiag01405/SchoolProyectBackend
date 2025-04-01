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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int userID)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID) // 🔹 Solo filtrar por UserID
                .ToListAsync();

            return Ok(notifications);
        }

        /*[HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int? userID)
        {
            IQueryable<Notification> query = _context.Notifications;

            // 🔹 Si se pasa userID, filtrar por ese usuario
            if (userID.HasValue)
            {
                query = query.Where(n => n.UserID == userID.Value);
            }

            var notifications = await query.ToListAsync();
            return Ok(notifications);
        }*/

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
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetNotification), new { id = notification.NotifyID }, notification);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateNotifications([FromBody] List<Notification> notifications)
        {
            if (notifications == null || notifications.Count == 0)
                return BadRequest("No hay notificaciones para registrar.");

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones creadas exitosamente.", count = notifications.Count });
        }

    }
}



