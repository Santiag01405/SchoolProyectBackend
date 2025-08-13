// Controllers/NurseController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.DTOs;
using SchoolProyectBackend.Models;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class NurseController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NurseController(ApplicationDbContext context)
    {
        _context = context;
    }

    // La autenticación se ha eliminado. Los datos de usuario se envían por la URL.
    [HttpPost("visit")]
    public async Task<IActionResult> PostNurseVisit([FromQuery] int nurseUserID, [FromQuery] int schoolID, [FromBody] NurseVisitCreateDto visitDto)
    {
        // Se valida que el usuario que hace la solicitud exista y sea un enfermero
        var nurseUser = await _context.Users.FirstOrDefaultAsync(u => u.UserID == nurseUserID && u.SchoolID == schoolID);
        if (nurseUser == null || nurseUser.RoleID != 4) // Asegúrate que el roleID para Nurse sea 4
        {
            return Unauthorized("Usuario no válido para realizar esta acción.");
        }

        // Busca al estudiante en la base de datos
        var studentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserID == visitDto.StudentUserID && u.SchoolID == schoolID);
        if (studentUser == null)
        {
            return NotFound("Estudiante no encontrado en esta escuela.");
        }

        // Busca la relación con el padre
        var parentRelationship = await _context.UserRelationships.FirstOrDefaultAsync(ur =>
            (ur.User1ID == studentUser.UserID || ur.User2ID == studentUser.UserID) &&
            ur.RelationshipType == "Padre-Hijo" &&
            ur.SchoolID == schoolID);

        // Crea el registro de la visita a la enfermería
        var nurseVisit = new NurseVisit
        {
            StudentUserID = visitDto.StudentUserID,
            VisitDate = System.DateTime.Now,
            Reason = visitDto.Reason,
            Treatment = visitDto.Treatment,
            NurseUserID = nurseUserID,
            SchoolID = schoolID
        };

        _context.NurseVisits.Add(nurseVisit);

        // Envía notificación al padre si la relación existe
        if (parentRelationship != null)
        {
            var parentUserID = (parentRelationship.User1ID == studentUser.UserID) ? parentRelationship.User2ID : parentRelationship.User1ID;

            var notification = new Notification
            {
                Title = "Visita a la Enfermería",
                Content = $"Su hijo(a) {studentUser.UserName} visitó la enfermería. Razón: {nurseVisit.Reason}. " +
                          $"Tratamiento: {nurseVisit.Treatment ?? "No se aplicó."}",
                Date = System.DateTime.Now,
                UserID = parentUserID,
                SchoolID = schoolID,
                IsRead = false
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(PostNurseVisit), new { id = nurseVisit.VisitID }, nurseVisit);
    }
}