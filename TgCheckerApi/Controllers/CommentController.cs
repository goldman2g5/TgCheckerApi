using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Services;
using TgCheckerApi.Models.PostModels;
using AutoMapper;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;
        private readonly IMapper _mapper;

        public CommentController(TgDbContext context, IMapper mapper, NotificationService notificationService)
        {
            _context = context;
            _userService = new UserService(context);
            _notificationService = notificationService;
            _mapper = mapper;
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

        // GET: api/Comment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            return comment;
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
        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpPost("Report/{id}")]
        public async Task<ActionResult<Channel>> ReportComment(int id, ReportPostModel report)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;
            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (_context.Reports == null)
            {
                return Problem("Entity set 'TgCheckerDbContext.Reports' is null.");
            }

            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }


            report.UserId = user.Id;
            report.ReportTime = DateTime.Now;
            report.ChannelId = comment.ChannelId;
            report.CommentId = id;
            report.ReportType = 2;

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var reportGetModel = _mapper.Map<ReportGetModel>(report);

            reportGetModel.ReporteeName = user.Username;



            return Ok();
        }

        // POST: api/Comment
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [RequiresJwtValidation]
        [BypassApiKey]
        [HttpPost]
        public async Task<ActionResult<Comment>> PostComment(CommentPostModel comment)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (_context.Comments == null)
            {
                return Problem("Entity set 'TgDbContext.Comments'  is null.");
            }

            comment.UserId = user.Id;
            comment.CreatedAt = DateTime.UtcNow;

            if (comment.ParentId == 0)
            {
                comment.ParentId = null;
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComment", new { id = comment.Id }, comment);
        }


        // DELETE: api/Comment/5
        [HttpPost("{id}")]
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
