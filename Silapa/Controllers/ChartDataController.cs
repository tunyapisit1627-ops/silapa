using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;

namespace Silapa.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChartDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChartDataController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("GetPieChartData")]
        public IActionResult GetPieChartData(int? s_id)
        {
            var data = _context.category
    .Include(x => x.competitionlists)
        .ThenInclude(x => x.registerheads)
        .ThenInclude(x => x.Registerdetail)
    .AsNoTracking()
    .Select(category => new
    {
        Name = category.Name,
        TotalParticipants = category.competitionlists
            .SelectMany(cl => cl.registerheads)
             .Where(rh => !s_id.HasValue || rh.s_id == s_id) // ตรวจสอบ s_id
            .SelectMany(rh => rh.Registerdetail)
            .Where(rd => rd.Type == "student") 
            .Count()
    })
    .ToList(); // ดึงข้อมูลทั้งหมดมาเก็บใน List
            // สร้าง Random สำหรับสุ่มสี
            var random = new Random();
            Func<string> getRandomColor = () =>
            {
                return $"#{random.Next(0x1000000):X6}"; // สร้าง Hex สี เช่น #FF5733
            };
            var chartData = new
            {
                labels = data.Select(b => b.Name).ToArray(),
                datasets = new[]
                {
                new
                {
                    data = data.Select(b => b.TotalParticipants).ToArray(),
                    backgroundColor = data.Select(_ => getRandomColor()).ToArray()
                }
            }
            };

            return Ok(chartData);
        }
    }
}