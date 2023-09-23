using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly TgDbContext _context;

        public CommentController(TgDbContext context)
        {
            _context = context;
        }

        // GET: api/Comment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
        {
            if (_context.Comments == null)
            {
                return NotFound();
            }
            return await _context.Comments.ToListAsync();
        }

        [HttpGet("{channelId}")]
        public async Task<ActionResult<List<CommentGetModel>>> GetCommentsByChannel(int channelId)
        {
            // Get all comments belonging to the channel, including the user details
            var comments = await _context.Comments
                .Where(c => c.ChannelId == channelId)
                .Include(c => c.User) // Include user details
                .ToListAsync();

            // Filter top-level comments
            var topLevelComments = comments
                .Where(c => c.ParentId == null)
                .ToList();

            // Initialize the result list
            var resultList = new List<CommentGetModel>();

            // Populate the CommentGetModel object, which includes replies
            foreach (var comment in topLevelComments)
            {
                var commentGetModel = new CommentGetModel
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    UserId = comment.UserId,
                    ChannelId = comment.ChannelId,
                    ParentId = comment.ParentId,
                    CreatedAt = comment.CreatedAt,
                    Username = comment.User?.Username ?? string.Empty, // Set username
                    Replies = comments
                        .Where(c => c.ParentId == comment.Id)
                        .ToList()
                };

                resultList.Add(commentGetModel);
            }

            return Ok(resultList);
        }

        // PUT: api/Comment/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, Comment comment)
        {
            if (id != comment.Id)
            {
                return BadRequest();
            }

            _context.Entry(comment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
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

        // POST: api/Comment
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Comment>> PostComment(Comment comment)
        {
            if (_context.Comments == null)
            {
                return Problem("Entity set 'TgDbContext.Comments'  is null.");
            }
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComment", new { id = comment.Id }, comment);
        }

        // DELETE: api/Comment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            if (_context.Comments == null)
            {
                return NotFound();
            }
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommentExists(int id)
        {
            return (_context.Comments?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
