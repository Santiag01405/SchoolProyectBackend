using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolProyectBackend.Controllers
{

    [ApiController]
    [Route("api/relationships")]
    public class UserRelationshipsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserRelationshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Crear relación entre usuarios (Ej: Padre-Hijo, Profesor-Estudiante)
        [HttpPost("create")]
        public async Task<IActionResult> CreateRelationship([FromBody] UserRelationship relationship)
        {
            if (relationship.User1ID == relationship.User2ID)
                return BadRequest("Un usuario no puede relacionarse consigo mismo.");

            // Validar existencia de usuarios
            var user1 = await _context.Users.FindAsync(relationship.User1ID);
            var user2 = await _context.Users.FindAsync(relationship.User2ID);

            if (user1 == null || user2 == null)
                return BadRequest("Uno de los usuarios no existe.");

            // Asignar el SchoolID en base a los usuarios relacionados (si no viene en el body)
            if (relationship.SchoolID == 0)
            {
                if (user1.SchoolID != user2.SchoolID)
                    return BadRequest("Los usuarios pertenecen a escuelas diferentes.");

                relationship.SchoolID = user1.SchoolID;
            }

            // Verificar duplicados
            bool exists = await _context.UserRelationships.AnyAsync(ur =>
                ur.User1ID == relationship.User1ID &&
                ur.User2ID == relationship.User2ID &&
                ur.RelationshipType == relationship.RelationshipType &&
                ur.SchoolID == relationship.SchoolID);

            if (exists)
                return BadRequest("La relación ya existe en esa escuela.");

            _context.UserRelationships.Add(relationship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Relación creada exitosamente." });
        }

        // Obtener padre(s) del estudiante en una escuela específica
        [HttpGet("user/{userId}/parents")]
        public async Task<IActionResult> GetParentsByUser(int userId, [FromQuery] int schoolId)
        {
            var parents = await _context.Users
                .Where(p => _context.UserRelationships
                    .Any(ur =>
                        ur.User1ID == userId &&
                        ur.User2ID == p.UserID &&
                        ur.RelationshipType == "Padre-Hijo" &&
                        ur.SchoolID == schoolId))
                .Select(p => new { p.UserID, p.UserName })
                .ToListAsync();

            return Ok(parents);
        }

        // Obtener estudiantes de un PROFESOR en una escuela específica
        [HttpGet("user/{userId}/students")]
        public async Task<IActionResult> GetStudentsByUser(int userId, [FromQuery] int schoolId)
        {
            var students = await _context.Users
                .Where(s => _context.UserRelationships
                    .Any(ur =>
                        ur.User2ID == userId &&
                        ur.User1ID == s.UserID &&
                        ur.RelationshipType == "Profesor-Estudiante" &&
                        ur.SchoolID == schoolId))
                .Select(s => new { s.UserID, s.UserName })
                .ToListAsync();

            return Ok(students);
        }

        // ******************** NUEVO ENDPOINT PARA PADRES AÑADIDO ********************
        // GET: api/relationships/user/{userId}/children
        // Obtiene los hijos (estudiantes) de un padre
        [HttpGet("user/{userId}/children")]
        public async Task<IActionResult> GetChildrenOfParent(int userId, [FromQuery] int schoolId)
        {
            var children = await _context.Users
                .Where(s => s.RoleID == 1 && // Asegurarnos de que solo buscamos estudiantes
                            _context.UserRelationships
                                .Any(ur =>
                                    ur.User2ID == userId &&           // UserID del Padre
                                    ur.User1ID == s.UserID &&         // UserID del Hijo (Estudiante)
                                    ur.RelationshipType == "Padre-Hijo" &&
                                    ur.SchoolID == schoolId))
                .Select(s => new {
                    UserID = s.UserID,
                    StudentName = s.UserName // Mapeamos UserName a StudentName para que coincida con el modelo del frontend
                })
                .ToListAsync();

            // Es mejor devolver una lista vacía que un 404 si el padre no tiene hijos registrados.
            // La app móvil puede manejar una lista vacía sin problemas.
            return Ok(children);
        }
        // ***************************************************************************
    }
}