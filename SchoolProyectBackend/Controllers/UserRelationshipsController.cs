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

            if (!_context.Users.Any(u => u.UserID == relationship.User1ID) ||
                !_context.Users.Any(u => u.UserID == relationship.User2ID))
            {
                return BadRequest("Uno de los usuarios no existe.");
            }

            if (_context.UserRelationships.Any(ur => ur.User1ID == relationship.User1ID
                                                  && ur.User2ID == relationship.User2ID
                                                  && ur.RelationshipType == relationship.RelationshipType))
            {
                return BadRequest("La relación ya existe.");
            }

            _context.UserRelationships.Add(relationship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Relación creada exitosamente." });
        }

        //Obtener padre del estudiante
        [HttpGet("user/{userId}/parents")]
        public async Task<IActionResult> GetParentsByUser(int userId)
        {
            var parents = await _context.Users
                .Where(p => _context.UserRelationships
                    .Any(ur => ur.User1ID == userId && ur.User2ID == p.UserID && ur.RelationshipType == "Padre-Hijo"))
                .Select(p => new { p.UserID, p.UserName })
                .ToListAsync();

            return Ok(parents);
        }

        //Obtener estudiantes
        [HttpGet("user/{userId}/students")]
        public async Task<IActionResult> GetStudentsByUser(int userId)
        {
            var students = await _context.Users
                .Where(s => _context.UserRelationships
                    .Any(ur => ur.User2ID == userId && ur.User1ID == s.UserID && ur.RelationshipType == "Profesor-Estudiante"))
                .Select(s => new { s.UserID, s.UserName })
                .ToListAsync();

            return Ok(students);
        }

    }

}