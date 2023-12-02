using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TgDbContext _context;

        public TagsController(TgDbContext context)
        {
            _context = context;
        }

        // GET: api/Tags
        [BypassApiKey]
        [HttpGet]
        public async Task<ActionResult<string>> GetTags()
        {
            var tags = await _context.Tags.Select(t => t.Text).ToListAsync();

            if (tags == null)
            {
                return NotFound();
            }

            string allTags = string.Join(",", tags);
            return allTags;
        }
    }
}
