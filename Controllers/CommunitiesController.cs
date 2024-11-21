using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reddit;
using Reddit.Dtos;
using Reddit.Models;

namespace Reddit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Communities
        [HttpGet] 
        public async Task<ActionResult<IEnumerable<Community>>> GetCommunities(int pageNumber = 1, int pageSize = 10, string? sortKey = "id",
             bool? isAscending = true, string? searchKey = null) 
        { 
            try 
            {
            // Start with base query
            IQueryable<Community> query = _context.Communities
                .Include(c => c.Posts)
                .Include(c => c.Subscribers);

            // Apply search filter if searchKey is provided
            if (!string.IsNullOrWhiteSpace(searchKey)) 
            {
                query = query.Where(c => 
                    c.Name.Contains(searchKey) || 
                    c.Description.Contains(searchKey));
            }
            
            query = ApplySorting(query, sortKey, isAscending);
            
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            Response.Headers.Add("X-Total-Count", totalItems.ToString());
            Response.Headers.Add("X-Total-Pages", totalPages.ToString());
            Response.Headers.Add("X-Current-Page", pageNumber.ToString()); 
            
            return Ok(items); 
            }
            
            catch (Exception ex) 
            {
            return StatusCode(500, $"Internal server error: {ex.Message}"); 
            } 
        }

   
        private IQueryable<Community> ApplySorting(IQueryable<Community> query, string? sortKey, bool? isAscending)
        {
        var ascending = isAscending ?? true;

        Expression<Func<Community, object>> keySelector = sortKey?.ToLower() switch
        {
            "createdat" => c => c.CreatedAt,
            "postscount" => c => c.Posts.Count,
            "subscriberscount" => c => c.Subscribers.Count,
            "id" or _ => c => c.Id
        };

        return ascending 
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector); 
        }

        // GET: api/Communities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Community>> GetCommunity(int id)
        {
            var community = await _context.Communities.FindAsync(id);

            if (community == null)
            {
                return NotFound();
            }

            return community;
        }

        // PUT: api/Communities/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCommunity(int id, Community community)
        {
            if (id != community.Id)
            {
                return BadRequest();
            }

            _context.Entry(community).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommunityExists(id))
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

        // POST: api/Communities
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Community>> PostCommunity(CommunityDto communityDto)
        {
            var community = communityDto.CreateCommunity();
            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCommunity", new { id = community.Id }, community);
        }

        // DELETE: api/Communities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunity(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community == null)
            {
                return NotFound();
            }

            _context.Communities.Remove(community);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommunityExists(int id)
        {
            return _context.Communities.Any(e => e.Id == id);
        }
    }
}
