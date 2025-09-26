using Microsoft.AspNetCore.SignalR;

namespace Silapa.Models
{
    public class ResultsHub : Hub
    {
        public async Task SendResult(string message)
         {
             await Clients.All.SendAsync("ReceiveResult", message);
         }
        // ใช้ส่งข้อมูลผลการแข่งขัน
        public async Task SendResults(List<ResultGroupViewModel> data)
        {
            await Clients.All.SendAsync("UpdateResults", data);
        }
    }
}