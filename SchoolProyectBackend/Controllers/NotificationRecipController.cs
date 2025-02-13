using Microsoft.AspNetCore.Mvc;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/notificationsRecip")]
    [ApiController]
    public class NotificationRecipController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationRecipController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationRecip>>> GetNotificationRecips()
        {
            return await _context.NotificationsRecip.ToListAsync();
        }

        [HttpGet("{recipientId}")]
        public async Task<ActionResult<NotificationRecip>> GetNotificationRecip(int recipientId)
        {
            var notificationRecip = await _context.NotificationsRecip.FindAsync(recipientId);
            if (notificationRecip == null) return NotFound();
            return notificationRecip;
        }

        [HttpPost]
        public async Task<ActionResult<NotificationRecip>> CreateNotificationRecip(NotificationRecip notificationRecip)
        {
            _context.NotificationsRecip.Add(notificationRecip);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetNotificationRecip), new { recipientId = notificationRecip.RecipientId }, notificationRecip);
        }
    }
}
