using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;

namespace Silapa.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("results")]
        public IActionResult GetResults(int competitionId)
        {
            var results = _context.Registerhead
         .Include(x => x.School)
             .Where(r => r.c_id == competitionId)
             .Select(r => new
             {
                 school = r.School.Name,
                 score = r.score,
                 level = r.rank, // Function to determine level based on score
                 notes = r.award
             })
             .OrderByDescending(r => r.level) // Optional: Order by score
             .ToList();

            return Ok(results);
        }
    }
}