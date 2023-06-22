using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelAccessController : ControllerBase
    {
        private readonly TgCheckerDbContext _context;

        public ChannelAccessController(TgCheckerDbContext context)
        {
            _context = context;
        }

        // GET: api/ChannelAccess
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelAccess>>> GetChannelAccesses()
        {
          if (_context.ChannelAccesses == null)
          {
              return NotFound();
          }
            return await _context.ChannelAccesses.ToListAsync();
        }

        // GET: api/ChannelAccess/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChannelAccess>> GetChannelAccess(int id)
        {
          if (_context.ChannelAccesses == null)
          {
              return NotFound();
          }
            var channelAccess = await _context.ChannelAccesses.FindAsync(id);

            if (channelAccess == null)
            {
                return NotFound();
            }

            return channelAccess;
        }

        // PUT: api/ChannelAccess/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChannelAccess(int id, ChannelAccess channelAccess)
        {
            if (id != channelAccess.Id)
            {
                return BadRequest();
            }

            _context.Entry(channelAccess).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelAccessExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ChannelAccess
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ChannelAccess>> PostChannelAccess(ChannelAccessPostModel channelAccess)
        {
          if (_context.ChannelAccesses == null)
          {
              return Problem("Entity set 'TgCheckerDbContext.ChannelAccesses'  is null.");
          }
            _context.ChannelAccesses.Add(channelAccess);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChannelAccess", new { id = channelAccess.Id }, channelAccess);
        }

        // DELETE: api/ChannelAccess/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannelAccess(int id)
        {
            if (_context.ChannelAccesses == null)
            {
                return NotFound();
            }
            var channelAccess = await _context.ChannelAccesses.FindAsync(id);
            if (channelAccess == null)
            {
                return NotFound();
            }

            _context.ChannelAccesses.Remove(channelAccess);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChannelAccessExists(int id)
        {
            return (_context.ChannelAccesses?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
