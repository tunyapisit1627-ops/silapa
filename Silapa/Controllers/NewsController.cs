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
        private readonly IWebHostEnvironment _webHostEnvironment;
        public NewsController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> frmnewslist()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserId = user.Id; // หรือ ViewBag.UserId = userId;

            var data = await _context.news
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(x => x.lastupdate)
            .ToListAsync();
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
        [ValidateAntiForgeryToken] // (แนะนำให้ใส่)
        public async Task<IActionResult> frmnewsadd(news model)
        {
            // ตั้งค่าผู้ใช้และสถานะ
            var user = await _userManager.GetUserAsync(User);
            model.m_id = user.m_id;
            model.u_id = user.Id;
            model.lastupdate = DateTime.Now;
            model.status = "1";

            // ----------------------------------------------------
            // (FIX 1) แก้ปัญหา Validation ตอนแก้ไข
            // ----------------------------------------------------
            if (model.id != 0)
            {
                ModelState.Remove(nameof(model.ImageFile));
                ModelState.Remove(nameof(model.GalleryFiles));
            }

            if (ModelState.IsValid)
            {
                // ----------------------------------------------------
                // 💡 2. (FIX 2) แยก Logic ระหว่าง Create และ Update
                // ----------------------------------------------------

                if (model.id == 0)
                {
                    // ========== CREATE (เพิ่มใหม่) ==========

                    // 1. จัดการรูปภาพหน้าปก (Cover Image)
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        model.CoverImageUrl = await SaveFileAsync(model.ImageFile, "cover");
                    }
                    _context.news.Add(model);
                    await _context.SaveChangesAsync(); // บันทึกเพื่อเอา model.id

                    // 2. จัดการรูปภาพแกลเลอรี (Gallery Images)
                    if (model.GalleryFiles != null && model.GalleryFiles.Length > 0)
                    {
                        await AddGalleryImagesAsync(model.id, model.GalleryFiles);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // ========== UPDATE (แก้ไข) ==========

                    // 1. โหลดข้อมูล "เก่า" จากฐานข้อมูล (แบบ Track)
                    var newsToUpdate = await _context.news.FindAsync(model.id);
                    if (newsToUpdate == null) return NotFound();

                    // 2. อัปเดตข้อมูล Text (จาก Form ที่ส่งมา)
                    newsToUpdate.titlename = model.titlename;
                    newsToUpdate.details = model.details;
                    newsToUpdate.Category = model.Category;
                    newsToUpdate.BadgeText = model.BadgeText;
                    newsToUpdate.lastupdate = DateTime.Now;
                    // (อัปเดต Field อื่นๆ ที่ต้องการ)

                    // 3. จัดการ "รูปปก" (Cover Image)
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        // ถ้ามีรูปใหม่มา:
                        // 3a. ลบรูปปกเก่า (ถ้ามี)
                        if (!string.IsNullOrEmpty(newsToUpdate.CoverImageUrl))
                        {
                            DeleteFile(newsToUpdate.CoverImageUrl);
                        }
                        // 3b. บันทึกรูปใหม่
                        newsToUpdate.CoverImageUrl = await SaveFileAsync(model.ImageFile, "cover");
                    }
                    // ถ้าไม่มีรูปใหม่มา (model.ImageFile == null): 
                    // 'newsToUpdate.CoverImageUrl' จะคงค่าเดิมไว้ (ไม่ถูกลบ)

                    // 4. จัดการ "แกลเลอรี" (Gallery)
                    if (model.GalleryFiles != null && model.GalleryFiles.Length > 0)
                    {
                        // ถ้ามีแกลเลอรีใหม่มา:
                        // 4a. ลบแกลเลอรีเก่าทั้งหมด (ทั้งไฟล์ และ ข้อมูลใน DB)
                        var oldGallery = await _context.NewsImages
                                            .Where(img => img.NewsId == model.id)
                                            .ToListAsync();

                        foreach (var oldImage in oldGallery)
                        {
                            DeleteFile(oldImage.ImageUrl);
                            _context.NewsImages.Remove(oldImage); // สั่งลบจาก DB
                        }

                        // 4b. เพิ่มแกลเลอรีใหม่
                        await AddGalleryImagesAsync(model.id, model.GalleryFiles);
                    }
                    // ถ้าไม่มีแกลเลอรีใหม่มา (model.GalleryFiles == null):
                    // แกลเลอรีเก่าจะยังคงอยู่ (ไม่ถูกลบ)

                    // 5. บันทึกการเปลี่ยนแปลงทั้งหมด
                    _context.news.Update(newsToUpdate);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("frmnewslist", "News");
            }

            return View(model);
        }
        // ฟังก์ชันช่วย: บันทึกไฟล์ และ คืนค่า Path ที่จะเก็บลง DB
        private async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            // 1. กำหนด Path (เช่น: wwwroot/images/news/cover)
            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "news", subFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 2. สร้างชื่อไฟล์ใหม่ไม่ซ้ำกัน
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string filePath = Path.Combine(folderPath, uniqueFileName);

            // 3. บันทึกไฟล์ลง Server
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 4. คืนค่า Path แบบ Relative (สำหรับเก็บลง DB)
            // (ผลลัพธ์: "images/news/cover/guid.jpg")
            return Path.Combine("images", "news", subFolder, uniqueFileName);
        }

        // ฟังก์ชันช่วย: ลบไฟล์
        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // (Optional) Log error
                _logger.LogError($"Error deleting file {filePath}: {ex.Message}");
            }
        }

        // ฟังก์ชันช่วย: เพิ่มรูปแกลเลอรีลง Context (ยังไม่ SaveChanges)
        private async Task AddGalleryImagesAsync(int newsId, IFormFile[] galleryFiles)
        {
            foreach (var galleryFile in galleryFiles)
            {
                if (galleryFile != null && galleryFile.Length > 0)
                {
                    // 1. บันทึกไฟล์
                    string imageUrl = await SaveFileAsync(galleryFile, "gallery");

                    // 2. สร้าง Entity
                    var newsImage = new NewsImage
                    {
                        NewsId = newsId,
                        FileName = Path.GetFileName(imageUrl), // หรือ galleryFile.FileName
                        ImageUrl = imageUrl
                    };

                    // 3. เพิ่มเข้า Context
                    _context.NewsImages.Add(newsImage);
                }
            }
        }
        /* public async Task<IActionResult> frmnewsdel(int id)
         {
             await _context.news.Where(x => x.id == id).ExecuteUpdateAsync(x => x.SetProperty(i => i.status, "0"));
             return RedirectToAction("frmnewslist", "News");
         }*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> frmnewsdel(int id)
        {
            // 1. ค้นหาข่าวที่ต้องการลบ
            var newsItem = await _context.news.FindAsync(id);
            if (newsItem == null)
            {
                return NotFound();
            }

            // --- ส่วนที่เพิ่มเข้ามา ---

            // 2. ค้นหารายการรูปภาพทั้งหมดที่ผูกกับข่าวนี้ (จากตาราง NewsImages)
            var galleryImages = await _context.NewsImages
                                          .Where(img => img.NewsId == id)
                                          .ToListAsync();

            // 3. วนลูปเพื่อลบ "ไฟล์" จริงใน Server
            if (galleryImages != null && galleryImages.Any())
            {
                foreach (var image in galleryImages)
                {
                    // สร้าง Path เต็มไปยังไฟล์ (เช่น C:\Project\wwwroot\images\news\gallery\guid.jpg)
                    // (เราใช้ ImageUrl เพราะตอนบันทึกคุณเก็บ Path แบบ "images/news/gallery/...")
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl);

                    try
                    {
                        // ตรวจสอบว่าไฟล์มีอยู่จริงหรือไม่ก่อนลบ
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // (Optional) Log error ไว้ เผื่อลบไฟล์ไม่ได้ แต่ยังคงทำงานต่อ
                        Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                    }

                    // 4. สั่งให้ EF "จด" ไว้ว่าต้องลบ "ข้อมูล" รูปภาพนี้ออกจากตาราง NewsImages
                    _context.NewsImages.Remove(image);
                }
            }

            // (Optional: ถ้าคุณมี "รูปปก" ที่เก็บแยกไว้ในตาราง News)
            // if (!string.IsNullOrEmpty(newsItem.CoverImageUrl))
            // {
            //     string coverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, newsItem.CoverImageUrl);
            //     if (System.IO.File.Exists(coverFilePath))
            //     {
            //         System.IO.File.Delete(coverFilePath);
            //     }
            // }

            // --- จบส่วนที่เพิ่มเข้ามา ---

            // 5. สั่งให้ EF "จด" ไว้ว่าต้องลบ "ข้อมูล" ข่าวหลัก
            _context.news.Remove(newsItem);

            // 6. บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล
            // (EF จะลบทั้ง News และ NewsImages ทั้งหมดใน Transaction เดียว)
            await _context.SaveChangesAsync();

            TempData["Message"] = "ลบข้อมูลข่าวและรูปภาพที่เกี่ยวข้องเรียบร้อยแล้ว";

            return RedirectToAction("frmnewslist", "News");
        }

        public async Task<IActionResult> frmList(int page = 1)
        {
            int pageSize = 12; // แสดง 12 รายการต่อหน้า
            var newsQuery = _context.news
                                    .Where(x => x.status == "1")
                                    .OrderByDescending(x => x.lastupdate);

            // 🚨 Logic สำหรับ Pagination
            int totalItems = await newsQuery.CountAsync();
            var newsList = await newsQuery
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(newsList);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> ToggleStatus(int id)
        {
            var newsItem = await _context.news.FindAsync(id);

            if (newsItem == null)
            {
                // ส่งค่ากลับไปบอกว่าไม่สำเร็จ
                return Json(new { success = false, message = "ไม่พบข้อมูลข่าว" });
            }

            try
            {
                // สลับสถานะเหมือนเดิม
                newsItem.status = (newsItem.status == "1") ? "0" : "1";
                newsItem.lastupdate = DateTime.Now;
                _context.Update(newsItem);
                await _context.SaveChangesAsync();

                // *** จุดที่เปลี่ยน ***
                // ส่งค่ากลับไปบอกว่าสำเร็จ พร้อมกับสถานะใหม่
                return Json(new { success = true, newStatus = newsItem.status });
            }
            catch
            {
                // ส่งค่ากลับไปบอกว่ามีข้อผิดพลาด
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
            }
        }
        [HttpPost] // 1. ใช้ Post เพื่อเปลี่ยนข้อมูล
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // 2. (แนะนำ) จำกัดสิทธิ์ให้ Admin เท่านั้น
        public async Task<IActionResult> TogglePin(int id)
        {
            var newsItem = await _context.news.FindAsync(id);
            if (newsItem == null)
            {
                return NotFound();
            }

            // 3. สลับค่า (ถ้า True เป็น False, ถ้า False เป็น True)
            newsItem.IsPinned = !newsItem.IsPinned;

            _context.Update(newsItem);
            await _context.SaveChangesAsync();

            // 4. กลับไปที่หน้ารายการข่าว
            return RedirectToAction(nameof(frmnewslist));
        }
    }
}