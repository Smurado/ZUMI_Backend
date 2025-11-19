using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZUMI_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjektController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjektController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/projekt
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjekte()
        {
            return await _context.Projekte
                .Include(p => p.Projektstatus)  // Beziehungen laden
                .Include(p => p.Sdgs)
                .ToListAsync();
        }

        // GET: api/projekt/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProjekt(Guid id)
        {
            var projekt = await _context.Projekte
                .Include(p => p.Projektstatus)
                .Include(p => p.Sdgs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projekt == null) return NotFound();
            return projekt;
        }

        // POST: api/projekt
        [HttpPost]
        public async Task<ActionResult<Project>> CreateProjekt(Project project)
        {
            _context.Projekte.Add(project);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProjekt), new { id = project.Id }, project);
        }

        // PUT: api/projekt/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProjekt(Guid id, Project project)
        {
            if (id != project.Id) return BadRequest();
            _context.Entry(project).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/projekt/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProjekt(Guid id)
        {
            var projekt = await _context.Projekte.FindAsync(id);
            if (projekt == null) return NotFound();
            _context.Projekte.Remove(projekt);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}