using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;

namespace Silapa.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        public NewsController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
        }
        public async Task<IActionResult> frmnewslist()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserId = user.Id; // หรือ ViewBag.UserId = userId;
            
            var data = await _context.news
            .Where(x => x.status == "1").ToListAsync();
            return View(data.OrderBy(x => x.id));
        }
        [HttpGet]
        public async Task<IActionResult> frmnewsadd(int id)
        {
            if (id == 0)
            {
                return View(new news());
            }
            else
            {
                return View(_context.news.Find(id));
            }

        }
        [HttpPost]
        public async Task<IActionResult> frmnewsadd(news model)
        {
            ModelState.Remove("ImageUrl");
            var user = await _userManager.GetUserAsync(User);
            model.m_id= user.m_id;
            model.u_id = user.Id;
            model.lastupdate = DateTime.Now;
            model.status = "1";
            if (ModelState.IsValid)
            {

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // ตั้งชื่อไฟล์และเส้นทางในการจัดเก็บไฟล์
                    var fileExtension = Path.GetExtension(model.ImageFile.FileName);
                    var newFileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine("wwwroot/images/uploads", newFileName);

                    // บันทึกไฟล์
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    // บันทึกเส้นทางไฟล์ลงในฐานข้อมูล
                    model.ImageUrl = "/images/uploads/" + newFileName;
                }

                // บันทึกข้อมูลอื่น ๆ ลงในฐานข้อมูล
                if (model.id == 0)
                {
                    _context.news.Add(model);
                }
                else
                {
                    _context.news.Update(model);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("frmnewslist", "News");

            }
            return View(model);
        }
        public async Task<IActionResult> frmnewsdel(int id){
            await _context.news.Where(x => x.id == id).ExecuteUpdateAsync(x=>x.SetProperty(i=>i.status,"0"));
            return RedirectToAction("frmnewslist", "News");
        }
    }
}