using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Silapa.Models;

namespace Silapa.Controllers
{
    public class ResultController : Controller
    {
        private readonly IHubContext<ResultsHub> _hubContext;

        public ResultController(IHubContext<ResultsHub> hubContext)
        {
            _hubContext = hubContext;
        }
        [HttpPost]
        public async Task<IActionResult> AnnounceResults([FromBody] AnnounceResultViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ResultMessage) || model.CompetitionId == 0)
            {
                return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
            }

            // ประกาศผลผ่าน SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveResult", $"รหัส {model.CompetitionId}: {model.ResultMessage}");

            return Json(new { success = true, message = "ประกาศผลเรียบร้อยแล้ว!" });
        }
    }
}