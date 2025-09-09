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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRelationshipById(int id)
        {
            var relationship = await _context.UserRelationships
                .Include(ur => ur.User1)
                .Include(ur => ur.User2)
                .FirstOrDefaultAsync(ur => ur.RelationID == id);

            if (relationship == null)
            {
                return NotFound("Relación no encontrada.");
            }

            return Ok(relationship);
        }

        // 🔹 Crear relación entre usuarios (Ej: Padre-Hijo, Profesor-Estudiante)
        /* [HttpPost("create")]
         public async Task<IActionResult> CreateRelationship([FromBody] UserRelationship relationship)
         {
             if (relationship.User1ID == relationship.User2ID)
                 return BadRequest("Un usuario no puede relacionarse consigo mismo.");

             var user1 = await _context.Users.FindAsync(relationship.User1ID); // Estudiante (convención actual)
             var user2 = await _context.Users.FindAsync(relationship.User2ID); // Padre
             if (user1 == null || user2 == null)
                 return BadRequest("Uno de los usuarios no existe.");

             // Determinar schoolId contextual de la relación
             int schoolIdForRelation;
             if (string.Equals(relationship.RelationshipType, "Padre-Hijo", StringComparison.OrdinalIgnoreCase))
             {
                 // Permitir sedes cruzadas y usar SIEMPRE la sede del estudiante
                 schoolIdForRelation = user1.SchoolID;
             }
             else
             {
                 // Política actual para otros tipos: exigir misma sede (o ajusta según tu negocio)
                 if (user1.SchoolID != user2.SchoolID)
                     return BadRequest("Los usuarios pertenecen a escuelas diferentes para este tipo de relación.");
                 schoolIdForRelation = user1.SchoolID;
             }

             relationship.SchoolID = relationship.SchoolID == 0 ? schoolIdForRelation : relationship.SchoolID;

             // Evitar duplicados
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
         }*/

        [HttpPost("create")]
        public async Task<IActionResult> CreateRelationship([FromBody] UserRelationship relationship)
        {
            if (relationship.User1ID == relationship.User2ID)
                return BadRequest("Un usuario no puede relacionarse consigo mismo.");

            var student = await _context.Users
                .Include(u => u.School)
                .FirstOrDefaultAsync(u => u.UserID == relationship.User1ID); // convención: User1 = estudiante

            var parent = await _context.Users
                .Include(u => u.School)
                .FirstOrDefaultAsync(u => u.UserID == relationship.User2ID); // convención: User2 = padre

            if (student == null || parent == null)
                return BadRequest("Uno de los usuarios no existe.");

            if (string.Equals(relationship.RelationshipType, "Padre-Hijo", StringComparison.OrdinalIgnoreCase))
            {
                var orgStu = student.School?.OrganizationID;
                var orgPar = parent.School?.OrganizationID;

                if (orgStu.HasValue && orgPar.HasValue)
                {
                    if (orgStu.Value != orgPar.Value)
                        return BadRequest("Los usuarios pertenecen a organizaciones distintas.");
                }
                else if (student.SchoolID != parent.SchoolID)
                {
                    return BadRequest("Los usuarios pertenecen a escuelas diferentes.");
                }

                relationship.SchoolID = student.SchoolID; // anclar a la sede del alumno
            }
            else
            {
                if (student.SchoolID != parent.SchoolID)
                    return BadRequest("Los usuarios pertenecen a escuelas diferentes para este tipo de relación.");
                relationship.SchoolID = student.SchoolID;
            }

            bool exists = await _context.UserRelationships.AnyAsync(ur =>
                ur.User1ID == relationship.User1ID &&
                ur.User2ID == relationship.User2ID &&
                ur.RelationshipType == relationship.RelationshipType &&
                ur.SchoolID == relationship.SchoolID);

            if (exists) return BadRequest("La relación ya existe en esa escuela.");

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


        // GET: api/relationships/user/{userId}/children
        // Obtiene los hijos (estudiantes) de un padre
        /*  [HttpGet("user/{userId}/children")]
          public async Task<IActionResult> GetChildrenOfParent(int userId, [FromQuery] int schoolId)
          {
              var children = await _context.Users
                  .Where(s => s.RoleID == 1 && // Asegurarnos de que solo buscamos estudiantes
                              _context.UserRelationships
                                  .Any(ur =>
                                      ur.User2ID == userId &&        
                                      ur.User1ID == s.UserID &&        
                                      ur.RelationshipType == "Padre-Hijo" &&
                                      ur.SchoolID == schoolId))
                  .Select(s => new {
                      UserID = s.UserID,
                      StudentName = s.UserName 
                  })
                  .ToListAsync();

              return Ok(children);
          }*/


        // Obtiene los hijos (estudiantes) de un padre
       // [HttpGet("user/{userId}/children")]
        /*  public async Task<IActionResult> GetChildrenOfParent(int userId, [FromQuery] int schoolId)
          {
              var children = await _context.UserRelationships
                  .Where(ur => ur.User2ID == userId &&
                               ur.RelationshipType == "Padre-Hijo" &&
                               ur.SchoolID == schoolId)
                  .Include(ur => ur.User1) // Incluimos el objeto de usuario completo del hijo
                  .Select(ur => new
                  {
                      RelationID = ur.RelationID,
                      UserID = ur.User1ID,
                      StudentName = ur.User1.UserName,
                      Email = ur.User1.Email // Agregamos el email para la vista
                  })
                  .ToListAsync();

              return Ok(children);
          }*/

        [HttpGet("user/{userId}/children")]
        public async Task<IActionResult> GetChildrenOfParent(int userId, [FromQuery] int? schoolId = null)
        {
            var parent = await _context.Users
                .Include(u => u.School)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (parent == null || parent.School == null)
                return NotFound("Padre o su escuela no encontrados.");

            int? parentOrgId = parent.School.OrganizationID;

            var q = _context.UserRelationships
                .Where(ur => ur.User2ID == userId && ur.RelationshipType == "Padre-Hijo")
                .Include(ur => ur.User1)
                    .ThenInclude(u => u.School)
                .AsQueryable();

            // Limitar por organización (si ambas están mapeadas)
            if (parentOrgId.HasValue)
                q = q.Where(ur => ur.User1.School.OrganizationID == parentOrgId);

            // Filtro opcional por sede
            if (schoolId.HasValue)
                q = q.Where(ur => ur.SchoolID == schoolId.Value);

            var children = await q.Select(ur => new ChildDto
            {
                RelationID = ur.RelationID,
                StudentUserID = ur.User1ID,
                StudentName = ur.User1.UserName,
                Email = ur.User1.Email,
                SchoolID = ur.User1.SchoolID,
                SchoolName = ur.User1.School != null ? ur.User1.School.Name : null
            }).ToListAsync();

            return Ok(children);
        }



        [HttpGet("school/{schoolId}")]
        public async Task<IActionResult> GetAllRelationshipsBySchool(int schoolId)
        {
            var relationships = await _context.UserRelationships
                .Where(ur => ur.SchoolID == schoolId)
                .Include(ur => ur.User1ID) // Cargar datos del primer usuario
                .Include(ur => ur.User2ID) // Cargar datos del segundo usuario
                .ToListAsync();

            if (relationships == null || !relationships.Any())
            {
                // Devolvemos un OK con una lista vacía si no hay relaciones,
                // ya que es un escenario válido.
                return Ok(new List<UserRelationship>());
            }
            return Ok(relationships);
        }

        // ***************************************************************************

        // PUT: api/relationships/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRelationship(int id, [FromBody] UserRelationship updatedRelationship)
        {
            if (id != updatedRelationship.RelationID)
            {
                return BadRequest("El ID de la ruta no coincide con el ID de la relación.");
            }

            var relationship = await _context.UserRelationships.FindAsync(id);
            if (relationship == null)
            {
                return NotFound("Relación no encontrada.");
            }

            // Validar que los usuarios de la relación actualizada existan
            var user1 = await _context.Users.FindAsync(updatedRelationship.User1ID);
            var user2 = await _context.Users.FindAsync(updatedRelationship.User2ID);

            if (user1 == null || user2 == null)
            {
                return BadRequest("Uno de los usuarios de la relación actualizada no existe.");
            }

            // Actualizar las propiedades de la relación existente
            relationship.User1ID = updatedRelationship.User1ID;
            relationship.User2ID = updatedRelationship.User2ID;
            relationship.RelationshipType = updatedRelationship.RelationshipType;
            relationship.SchoolID = updatedRelationship.SchoolID;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Relación actualizada correctamente." });
        }

        // 🆕 **NUEVO ENDPOINT**: Eliminar una relación por ID
        // DELETE: api/relationships/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRelationship(int id)
        {
            var relationship = await _context.UserRelationships.FindAsync(id);
            if (relationship == null)
            {
                return NotFound("Relación no encontrada.");
            }

            _context.UserRelationships.Remove(relationship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Relación eliminada correctamente." });
        } 
    }
}