using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Silapa.Models;
using Syncfusion.EJ2.Linq;
namespace Silapa.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ManagerController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
            _env = env;
        }


        public async Task<IActionResult> frmmanagerRegister(string c_id)
        {
            var user = await _userManager.GetUserAsync(User);
            var m_id = user.m_id;
            int a = Convert.ToInt32(c_id);
            List<int> m_idList = m_id.Split(',')
                          .Select(int.Parse)
                          .ToList();
            /* var data = _context.Competitionlist
             .Include(x => x.registerheads)
             .ThenInclude(x => x.Registerdetail)
                    .Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value))
                    .AsNoTracking()
                    .ToList();
             if (c_id != null)
             {
                 data = data.Where(x => x.c_id == a).ToList();
             }*/
            // ดึงข้อมูลการแข่งขันที่ตรงตามเงื่อนไข
            var competitions = _context.Competitionlist
        .Where(c => c.status == "1" && c.c_id.HasValue && m_idList.Contains(c.c_id.Value))
        .Select(c => new CompetitionViewModel1
        {
            Id = c.Id,
            c_id = (int)c.c_id,
            Name = c.Name,
            Type = c.type,
            TeamCount = c.registerheads.Count(rh => rh.status != "0"),
            StudentCount = c.registerheads
                .Where(rh => rh.status != "0")
                .SelectMany(rh => rh.Registerdetail)
                .Count(rd => rd.Type == "student"),
            TeacherCount = c.registerheads
                .Where(rh => rh.status != "0")
                .SelectMany(rh => rh.Registerdetail)
                .Count(rd => rd.Type == "teacher"),
            Status = c.status
        });
            if (c_id != null)
            {
                competitions = competitions.Where(c => c.c_id == a);
            }
            var data = competitions.ToList();

            var datacategory = await _context.category.Where(x => x.status == "1" && m_idList.Contains(x.Id)).ToListAsync();

            TempData["LevelData"] = new SelectList(datacategory, "Id", "Name");
            ViewBag.currentTypelevel = a;
            /*  var datah = competitions.Where(x => x.registerheads.Any(r => r.status != "0")).ToList();
              if (datah.Count > 0)
              {
                  ViewBag.data = datah;
                  // ViewBag.registerdetails = await _context.Registerdetail.Where(x => x.h_id == datah[0].id).ToListAsync();
              }
  */
            return View(competitions);
        }
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> frmmanagerscore(int c_id, int g_id)
        {

            ViewBag.c_id = c_id;
            var user = await _userManager.GetUserAsync(User);
            var m_id = user.m_id;
            int a = Convert.ToInt32(c_id);
            TempData["LevelData"] = new SelectList(_context.grouplist.ToList(), "Id", "Name");
            ViewBag.currentTypelevel = g_id;
            List<int> m_idList = m_id.Split(',')
                          .Select(int.Parse)
                          .ToList();
            var datah = await _context.Registerhead.Where(x => x.c_id == c_id && x.status != "0")
            .AsNoTracking()
            .Include(x => x.Competitionlist)
            .Include(x => x.School)
            .ThenInclude(x => x.grouplist)
            .ToListAsync();
            if (datah.Count > 0)
            {
                if (g_id != 0)
                {
                    datah = datah.Where(x => x.School.g_id == g_id).ToList();
                }
                /*  if (datah[0].status == "1")
                  {
                      datah = datah.OrderBy(x => x.id).ToList();
                  }
                  else if (datah[0].status == "2")
                  {
                      datah = datah.OrderByDescending(x => x.score).ToList();
                  }*/
                datah = datah[0].status switch
                {
                    "1" => datah.OrderBy(x => x.id).ToList(),
                    "2" => datah.OrderByDescending(x => x.score).ToList(),
                    _ => datah
                };


            }
            var registeredSchoolIds = datah
                                 .Select(r => r.s_id)
                                 .ToList();
            var schoolList = _context.school
   .Where(x => x.status == "1" && !registeredSchoolIds.Contains(x.Id)) // เงื่อนไข
   .Select(x => new SelectListItem
   {
       Value = x.Id.ToString(),
       Text = x.Name
   })
   .ToList();
            ViewBag.SchoolList = schoolList;
            ViewBag.fileupload = await _context.uploadfilepdf.ToListAsync();
            return View(datah);
        }
        public async Task<IActionResult> GetPdfDetails(int c_id, int s_id)
        {
            // ค้นหาข้อมูล PDF ตามเงื่อนไข c_id และ s_id
            var pdfDetails = await _context.uploadfilepdf
                .Where(d => d.c_id == c_id && d.s_id == s_id)
                .FirstOrDefaultAsync();

            if (pdfDetails == null)
            {
                return Content("ไม่พบข้อมูล PDF");
            }

            // แสดงข้อมูลที่ต้องการใน modal
            return PartialView("_PdfDetailsPartial", pdfDetails);
        }
        [HttpPost]
        public async Task<IActionResult> RejectPdf([FromBody] RejectReasonModel model)
        {
            if (ModelState.IsValid)
            {
                // ค้นหาและบันทึกเหตุผลในฐานข้อมูล
                var pdfRecord = await _context.uploadfilepdf
                    .Where(d => d.c_id == model.c_id && d.s_id == model.s_id)
                    .FirstOrDefaultAsync();

                if (pdfRecord != null)
                {
                    pdfRecord.msg = model.Reason;
                    pdfRecord.status = "0";
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "ไม่พบข้อมูล PDF" });
            }

            return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
        }
        [HttpPost]
        public async Task<IActionResult> ApprovePdf([FromBody] ApprovePdfModel model)
        {
            if (ModelState.IsValid)
            {
                // ค้นหาและอัปเดตสถานะเอกสารเป็น "ผ่านเกณฑ์"
                var pdfRecord = await _context.uploadfilepdf
                    .Where(d => d.c_id == model.c_id && d.s_id == model.s_id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (pdfRecord != null)
                {
                    // pdfRecord.status = "2";  // หรือใช้สัญลักษณ์อื่นที่ต้องการ
                    //await _context.SaveChangesAsync();
                    await _context.uploadfilepdf.Where(x => x.id == pdfRecord.id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(x => x.status, "2")
                    );

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "ไม่พบข้อมูล PDF" });
            }

            return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
        }
        [HttpPost]
        public IActionResult SaveScore([FromBody] ScoreViewModel model)
        {
            var record = _context.Registerhead.FirstOrDefault(s => s.id == model.Id);
            if (record != null)
            {
                _context.Registerhead.Where(x => x.id == record.id && x.status != "0").ExecuteUpdate(
                    x => x.SetProperty(i => i.score, model.Score)
                );
                return Ok();
            }
            return BadRequest();
        }
        [HttpPost]
        public async Task<IActionResult> AnnounceResults(int c_id, string s)
        {
            // กระบวนการประกาศผล เช่น อัพเดทสถานะ หรือคำนวณคะแนน
            // ตัวอย่างเช่น อัพเดทสถานะในฐานข้อมูลเป็น "ประกาศผลแล้ว"
            if (s == "1")
            {
                await _context.Registerhead.Where(x => x.c_id == c_id)
                .ExecuteUpdateAsync(x => x.SetProperty(i => i.status, s));
                return Json(new { success = true, c_id = c_id, message = "ได้ทำการยกเลิกประกาศเรียบร้อยแล้ว" });
            }
            var user = await _userManager.GetUserAsync(User);
            var items = _context.Registerhead
                         .Where(x => x.c_id == c_id && x.status == "1") // เฉพาะสถานะที่ต้องการ
                         .OrderByDescending(x => x.score)
                         .ToList();
            if (!items.Any())
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลสำหรับประกาศผล" });
            }
            int rank = 1;
            decimal? previousScore = null;
            bool isTieForFirstPlace = false;
            await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in items)
                {
                    // กำหนดรางวัลตามคะแนน
                    if (item.score == -1)
                    {
                        item.award = "ไม่ได้แข่งขัน";
                        item.rank = null; // ไม่กำหนดลำดับ
                        continue;
                    }
                    else
                    {
                        item.award = item.score >= 80 ? "เหรียญทอง" :
                                     item.score >= 70 ? "เหรียญเงิน" :
                                     item.score >= 60 ? "เหรียญทองแดง" :
                                     "เข้าร่วม";
                        if (item.score != previousScore)
                        {
                            item.rank = rank;
                            rank++;
                        }
                        else
                        {
                            item.rank = rank - 1; // กรณีคะแนนเท่ากัน
                            if (rank == 2) // ถ้าคะแนนอันดับ 1 เท่ากัน
                            {
                                isTieForFirstPlace = true;
                            }
                        }

                        previousScore = item.score;
                    }
                    item.status = s;
                }
                if (isTieForFirstPlace)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return Json(new { success = false, message = "พบคะแนนอันดับ 1 เท่ากัน กรุณาตรวจสอบข้อมูลอีกครั้ง!" });
                }
                _context.UpdateRange(items);
                await _context.SaveChangesAsync();
                await _context.Database.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _context.Database.RollbackTransactionAsync();
                return Json(new { success = false, message = ex.Message });
            }


            // ส่งกลับไปยังหน้าแสดงผลหลังประกาศสำเร็จ
            // return RedirectToAction("ResultsPage");
            return Json(new { success = true, c_id = c_id, message = "ประกาศผลสำเร็จ!" });
            // return RedirectToAction("frmmanagerscore", new { c_id = c_id });
        }
        // หน้าที่จะแสดงผลหลังจากประกาศผลแล้ว
        public IActionResult ResultsPage(int c_id)
        {
            var results = _context.Registerhead.Where(x => x.c_id == c_id).Include(x => x.School).ThenInclude(x => x.grouplist).OrderByDescending(x => x.score).ToList();
            return View(results);
        }
        public async Task<IActionResult> frmracedetails()
        {
            var user = await _userManager.GetUserAsync(User);

            List<int> m_idList = user.m_id.Split(',')
                          .Select(int.Parse)
                          .ToList();
            var data = _context.Competitionlist.Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value)).ToList();
            var datarace = _context.racedetails.Where(x => x.status == "1" && x.Competitionlist.c_id.HasValue && m_idList.Contains(x.Competitionlist.c_id.Value)).Include(x => x.Racelocation).ToList();

            ViewBag.race = datarace;
            return View(data.OrderBy(x => x.Id));
        }
        public async Task<IActionResult> frmracedetailsAdd(int id, int c_id, int r_id, string c_name)
        {
            ViewBag.locationData = new SelectList(_context.Racelocation.ToList(), "id", "name");
            ViewBag.currentTypelocation = r_id;
            @ViewBag.c_name = c_name;
            @ViewBag.c_id = c_id;
            if (id == 0)
            {
                return View(new racedetails());
            }
            else
            {
                return View(_context.racedetails.Find(id));
            }
        }
        [HttpPost]
        public async Task<IActionResult> frmracedetailsAdd([Bind("id,c_id,r_id,building,room,daterace,time,details,status")] racedetails data, string c_name)
        {
            var user = await _userManager.GetUserAsync(User);
            data.lastupdate = DateTime.Now;
            data.u_id = user.Id;
            if (ModelState.IsValid)
            {
                if (data.id == 0)
                {
                    data.status = "1";
                    _context.Add(data);
                    _context.SaveChanges();
                }
                else
                {
                    await _context.racedetails.Where(x => x.id == data.id).ExecuteUpdateAsync(
                     x => x.SetProperty(i => i.r_id, data.r_id)
                     .SetProperty(i => i.building, data.building)
                     .SetProperty(i => i.room, data.room)
                     .SetProperty(i => i.daterace, data.daterace)
                     .SetProperty(i => i.details, data.details)
                     .SetProperty(i => i.time, data.time)
                     .SetProperty(i => i.u_id, user.Id)
                     .SetProperty(i => i.lastupdate, DateTime.Now)
                    );
                }
                return RedirectToAction("frmracedetails");
            }
            ViewBag.locationData = new SelectList(_context.Racelocation.ToList(), "id", "name");
            ViewBag.currentTypelocation = data.r_id;
            @ViewBag.c_name = c_name;
            @ViewBag.c_id = data.c_id;
            return View(data);
        }

        public async Task<IActionResult> frmdirectorlist()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                          .Select(int.Parse)
                          .ToList();
            var data = await _context.referee.Where(x => m_idList.Contains(x.m_id) && x.c_id == 0).ToListAsync();
            return View(data.OrderBy(x => x.id));
        }
        [HttpGet]
        public async Task<IActionResult> frmdirectoradd1(int id, int g_id, string c_name, string r_name, string duty)
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = new List<int> { 31 }; // สร้างลิสต์ใหม่โดยมี 0 เป็นตัวแรก
            m_idList.AddRange(
                user.m_id.Split(',')
                         .Select(int.Parse)
            );
            ViewBag.list = await _context.category.Where(x => m_idList.Contains(x.Id)).ToListAsync();
            ViewBag.c_name = c_name;
            ViewBag.r_name = r_name;
            ViewBag.c_id = id;
            ViewBag.g_id = g_id;
            ViewBag.duty = duty;
            var data = await _context.referee.Where(x => x.m_id == id && x.g_id == g_id).ToListAsync();
            return View(data.OrderBy(x => x.id));


        }
        [HttpPost]
        public async Task<IActionResult> SaveRefereeAsync([FromBody] RefereeViewModel model)
        {
            var urlpic = "";
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = new List<int> { 31 }; // สร้างลิสต์ใหม่โดยมี 0 เป็นตัวแรก
            m_idList.AddRange(
                user.m_id.Split(',')
                         .Select(int.Parse)
            );
            if (ModelState.IsValid)
            {
                var totalLimitData = await _context.groupreferee
     .Where(x => x.c_id == model.c_id && x.id == model.g_id)
     .Select(x => new { Total = x.total })
     .FirstOrDefaultAsync();

                if (totalLimitData == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการกำหนดจำนวน" });
                }

                var existingCount = await _context.referee
                    .CountAsync(x => x.g_id == model.g_id);

                if (existingCount + model.Categories.Count() > totalLimitData.Total)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"ไม่สามารถเพิ่มข้อมูลได้เนื่องจากเต็มจำนวนที่กำหนดแล้ว ({existingCount}/{totalLimitData.Total})",
                        current = existingCount,
                        limit = totalLimitData.Total
                    });
                }
                // Assuming the 'image' field is a base64-encoded string
                if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    var base64Image = model.ImageUrl.Split(',')[1]; // Remove the base64 prefix
                    var imageBytes = Convert.FromBase64String(base64Image);
                    var fileName = $"image_{Guid.NewGuid()}.jpg";

                    // Save the image to your file system or database (e.g., using a file storage service)
                    var filePath = Path.Combine("wwwroot/uploads", fileName);
                    System.IO.File.WriteAllBytes(filePath, imageBytes);
                    urlpic = "/uploads/" + fileName;
                }
                else
                {
                    if (model.id == 0)
                    {
                        urlpic = "/uploads/no-image-icon-4.png";
                    }
                    else
                    {
                        urlpic = _context.referee.Where(x => x.id == model.id).FirstOrDefault().ImageUrl;
                    }
                }
                await _context.Database.BeginTransactionAsync();
                var datagroupreferee = await _context.groupreferee.AsNoTracking().Where(x => m_idList.Contains(x.c_id)).ToListAsync();
                if (model.id == 0)
                {
                    foreach (var item in model.Categories)
                    {
                        int g_id;
                        var sql = datagroupreferee.Where(x => x.c_id == item && x.name == model.r_name.ToString().Trim()).FirstOrDefault();
                        if (sql == null)
                        {
                            var datasql = new groupreferee
                            {
                                c_id = item,
                                name = model.r_name,
                                duty = model.duty
                            };
                            _context.groupreferee.Add(datasql);
                            await _context.SaveChangesAsync();
                            g_id = datasql.id;
                        }
                        else
                        {
                            g_id = sql.id;
                        }

                        var data = new referee
                        {
                            id = model.id,
                            name = model.name,
                            position = model.position,
                            role = model.role,
                            u_id = user.Id,
                            m_id = item,
                            c_id = 0,
                            ImageUrl = urlpic,
                            g_id = g_id,
                            status = "1",
                            lastupdate = DateTime.Now
                        };

                        _context.referee.Add(data);
                    }
                    _context.SaveChanges();
                }
                else
                {
                    await _context.referee.Where(x => x.id == model.id).ExecuteUpdateAsync(
                     x => x.SetProperty(i => i.position, model.position)
                     .SetProperty(i => i.role, model.role)
                     .SetProperty(i => i.name, model.name)
                     .SetProperty(i => i.ImageUrl, urlpic)
                     .SetProperty(i => i.lastupdate, DateTime.Now)
                    );
                }
                try
                {

                    await _context.Database.CommitTransactionAsync();
                    // Save other fields like name, duty, etc.
                    // Save logic here...

                    return Json(new { success = true, message = "Data saved successfully." });
                }
                catch (Exception ex)
                {
                    await _context.Database.RollbackTransactionAsync();
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                    {
                        try
                        {
                            var filePath = Path.Combine("wwwroot", urlpic);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        catch (Exception eex)
                        {
                            // Log กรณีการลบไฟล์ล้มเหลว
                            Console.WriteLine($"Error deleting file: {eex.Message}");
                        }
                    }
                    return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
                }

            }

            return Json(new { success = false, message = "Invalid data." });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteReferee1([FromBody] JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("id", out JsonElement idElement))
                {
                    int id = idElement.GetInt32();
                    var referee = await _context.referee.FindAsync(id);
                    if (referee == null)
                        return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการลบ" });

                    if (!string.IsNullOrEmpty(referee.ImageUrl))
                    {
                        if (referee.ImageUrl != "/uploads/no-image-icon-4.png")
                        {
                            var fullPath = Path.Combine(_env.WebRootPath, referee.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath); // ลบไฟล์รูปภาพ
                            }
                        }
                    }

                    // ลบข้อมูลจากฐานข้อมูล
                    _context.referee.Remove(referee);
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true, message = "ลบข้อมูลสำเร็จ" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการลบข้อมูล" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRefereeById(int id)
        {
            var referee = await _context.referee
                .Where(r => r.id == id)
                .Select(r => new
                {
                    id = r.id,
                    name = r.name,
                    role = r.role,
                    position = r.position,
                    imageUrl = r.ImageUrl
                }).FirstOrDefaultAsync();

            if (referee == null)
                return Json(new { success = false, message = "ไม่พบข้อมูล" });

            return Json(new { success = true, data = referee });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> frmdirectoradd(referee model, IFormFile ImageUrl)
        {
            ModelState.Remove("ImageUrl");
            var user = await _userManager.GetUserAsync(User);
            model.u_id = user.Id;
            model.c_id = 0;
            model.lastupdate = DateTime.Now;
            model.status = "1";
            if (ImageUrl == null)
            {
                model.ImageUrl = "/uploads/no-image-icon-4.png";
            }

            if (ModelState.IsValid)
            {
                // ตรวจสอบการอัปโหลดไฟล์
                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    // ตรวจสอบประเภทของไฟล์
                    var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(ImageUrl.FileName).ToLowerInvariant();

                    if (!permittedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageUrl", "กรุณาอัปโหลดเฉพาะไฟล์รูปภาพที่มีนามสกุล .jpg, .jpeg, .png, .gif เท่านั้น");
                        return View(model);
                    }

                    // ตั้งค่า path และชื่อไฟล์ใหม่ด้วย GUID
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    var newFileName = Guid.NewGuid().ToString();
                    var newFilePath = Path.ChangeExtension(newFileName, extension); // เปลี่ยนชื่อไฟล์ตาม GUID พร้อมนามสกุล

                    // สร้างโฟลเดอร์หากยังไม่มี
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // อัปโหลดไฟล์
                    var fullPath = Path.Combine(uploadsFolder, newFilePath);
                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(fileStream);
                    }

                    // บันทึก path ของรูปภาพลงในโมเดล
                    model.ImageUrl = "/uploads/" + newFilePath;
                }

                // บันทึกโมเดลลงในฐานข้อมูล (สมมติว่ามี DbContext อยู่แล้ว)
                _context.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("frmdirectorlist");
            }
            List<int> m_idList = user.m_id.Split(',')
                          .Select(int.Parse)
                          .ToList();
            var datacategory = await _context.category.Where(x => x.status == "1" && m_idList.Contains(x.Id)).ToListAsync();
            ViewBag.categoryData = new SelectList(datacategory, "Id", "Name");
            return View(model);

        }
        public async Task<IActionResult> frmRegister_addreferee(int id, int c_id, string c_name, string type, string d)
        {
            ViewBag.c_id = c_id;
            ViewBag.c_name = c_name;
            ViewBag.type = type;
            ViewBag.d = d;
            ViewBag.m_id = id;
            ViewBag.referees = await _context.referee.Where(x => x.c_id == c_id).ToListAsync();
            var data = await _context.Competitionlist.Where(x => x.Id == c_id).FirstOrDefaultAsync();
            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> Savereferees([FromBody] List<referee> teachers)
        {
            var user = await _userManager.GetUserAsync(User);
            if (teachers == null || teachers.Count == 0)
                return BadRequest("ไม่มีข้อมูลสำหรับบันทึก");

            // บันทึกข้อมูลลงฐานข้อมูล (เพิ่มหรืออัปเดตข้อมูลตาม Id)
            foreach (var teacher in teachers)
            {
                teacher.u_id = user.Id;
                // teacher.m_id = ; //user.m_id;
                if (teacher.id == 0) // ถ้า Id เป็น 0 ถือว่าเป็นการเพิ่มข้อมูลใหม่
                {
                    await _context.referee.AddAsync(teacher);
                }
                else // ถ้า Id ไม่ใช่ 0 ถือว่าเป็นการแก้ไขข้อมูลที่มีอยู่
                {
                    _context.referee.Update(teacher);
                }
            }
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Results announced successfully!",
                redirectUrl = Url.Action("frmmanagerscore", "Manager", new { c_id = teachers[0].c_id }) // Generate the redirect URL
            });
        }
        [HttpPost]
        public IActionResult Deleteferee(int id)
        {
            try
            {
                // เรียกใช้ service หรือ repository ของคุณเพื่อลบ referee ตาม ID
                var referee = _context.referee.Find(id); // สมมุติว่าคุณมี DbContext ชื่อ _context
                if (referee != null)
                {
                    var imagePath = referee.ImageUrl; // หรือเปลี่ยนให้ตรงกับฟิลด์ในโมเดลของคุณ

                    _context.referee.Remove(referee);
                    _context.SaveChanges(); // บันทึกการเปลี่ยนแปลงในฐานข้อมูล

                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath); // ลบไฟล์รูปภาพ
                        }
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // บันทึกข้อผิดพลาดถ้าต้องการ
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> frmapprovedirector(int c_id)
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            var datacategory = await _context.category.Where(x => x.status == "1" && m_idList.Contains(x.Id)).ToListAsync();
            ViewBag.categoryData = new SelectList(datacategory, "Id", "Name");
            var data = await _context.registerdirector.Where(x => x.status == "1" && m_idList.Contains(x.g_id)).Include(x => x.Competitionlist).ToListAsync();
            if (c_id != 0)
            {
                data = data.Where(x => x.g_id == c_id).ToList();
            }
            return View(data.OrderBy(x => x.id));
        }
        [HttpPost]
        public async Task<JsonResult> Approvedirector(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // ค้นหารายการตาม id
                _context.Database.BeginTransaction();
                var data = _context.registerdirector.Find(id);
                if (data != null)
                {
                    int countdirector = _context.Competitionlist.Where(x => x.Id == data.c_id).FirstOrDefault().director;
                    int countregister = _context.referee.Count(x => x.c_id == data.c_id && x.status == "1");
                    if (countregister >= countdirector)
                    {
                        return Json(new { success = false, message = "ไม่สามารถอนุมัติกรรมการได้เนื่องจากจำนวนเกิน " + countdirector + " คนแล้วถ้าต้องการขยายข้อมูลกรรมการกรุณาติดต่อ แอดมิน" });
                    }
                    data.status = "2"; // หรือสถานะที่ต้องการ

                    var ViewModel = new referee
                    {
                        name = data.name,
                        role = "กรรมการ",
                        position = data.position,
                        ImageUrl = data.ProfileImageUrl,
                        u_id = user.Id,
                        m_id = data.g_id,
                        c_id = data.c_id,
                        g_id = 0,
                        status = "1",
                        lastupdate = DateTime.Now
                    };

                    _context.referee.Add(ViewModel);
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "ไม่พบข้อมูลการสมัครกรรมการที่ต้องการอนุมัติ" });
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> frmprintAll()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            var datacategory = await _context.category.Where(x => x.status == "1" && m_idList.Contains(x.Id)).ToListAsync();
            ViewBag.categoryData = new SelectList(datacategory, "Id", "Name");
            var data = _context.Competitionlist
                  .Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value))
                  .ToList();
            return View(data.OrderBy(x => x.Id));
        }
        public async Task<IActionResult> frmcharts()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            var data = await _context.Registerhead
                           .Where(x => m_idList.Contains(x.c_id) && x.status != "0")
                           .Include(x => x.Registerdetail)
                           .ToListAsync();
            ViewBag.StudentCount = data.Select(x => x.Registerdetail.Count(rd => rd.Type == "student")).ToList().Count();
            ViewBag.TeacherCount = data.Select(x => x.Registerdetail.Count(rd => rd.Type == "teacher")).ToList().Count();
            ViewBag.CommitteeCount = _context.referee.Count(x => x.c_id.HasValue && m_idList.Contains(x.c_id.Value));
            ViewBag.OperationCommitteeCount = _context.referee.Count(x => m_idList.Contains(x.m_id) && x.status != "0" && x.c_id == 0);

            ViewBag.Competitionlist = await _context.Competitionlist.Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value)).Include(x => x.registerheads).ThenInclude(x => x.Registerdetail).Include(x => x.racedetails).ToListAsync();
            ViewBag.referee = await _context.referee.Where(x => m_idList.Contains(x.m_id) && x.status != "0" && x.c_id == 0).ToListAsync();
            return View();
        }
        public async Task<IActionResult> frmrefereeprintpic()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            //var data = await _context.Competitionlist.Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value)).Include(x => x.registerheads).ThenInclude(x => x.Registerdetail).Include(x => x.racedetails).ToListAsync();
            //ViewBag.referee = await _context.referee.Where(x => m_idList.Contains(x.m_id) && x.status != "0" && x.c_id != 0).ToListAsync();
            var datagroupreferee = await _context.groupreferee.Where(x => m_idList.Contains(x.c_id))
            .Include(x => x.referees)
            .AsNoTracking()
            .ToListAsync();
            ViewBag.groupreferee = datagroupreferee;
            var data = await _context.Competitionlist.Where(x => x.c_id.HasValue && m_idList.Contains(x.c_id.Value) && x.status == "1")
            .Include(x => x.referees)
            .AsNoTracking()
            .ToListAsync();
            return View(data.OrderBy(x => x.Id));
        }
        [HttpPost]
        public async Task<IActionResult> UpdateImagerefer(string croppedImage, int id)
        {
            // Process the croppedImage data, e.g., convert to byte array and save to server
            var newImageUrl = await SaveCroppedImage(croppedImage, id); // Method to save image and return URL

            // Update the database record with the new image URL
            var record = await _context.referee.Where(x => x.id == id).FirstOrDefaultAsync();
            if (record != null)
            {
                record.ImageUrl = newImageUrl;
                await _context.referee.Where(x => x.id == id)
                .ExecuteUpdateAsync(x => x.SetProperty(x => x.ImageUrl, newImageUrl));
            }
            return Json(new { success = true, newImageUrl });
        }
        private async Task<string> SaveCroppedImage(string base64Image, int id)
        {
            var imageBytes = Convert.FromBase64String(base64Image.Split(',')[1]);
            var fileName = $"image_refer_{id}.jpg";
            var filePath = Path.Combine("wwwroot/images", fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/images/{fileName}"; // Return the URL to be saved in the database
        }
        public async Task<IActionResult> frmgroupreferee()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = new List<int> { 31 }; // สร้างลิสต์ใหม่โดยมี 0 เป็นตัวแรก
            m_idList.AddRange(
                user.m_id.Split(',')
                         .Select(int.Parse)
            );
            var data = await _context.groupreferee.Where(x => m_idList.Contains(x.c_id)).Include(x => x.Category).ToListAsync();

            if (data.Count == 0)
            {
                var cdata = await _context.category.Where(x => x.status == "1" && x.Id != 1 && x.Id != 2 && x.Id != 3 && x.Id != 4 && x.Id != 5 && x.Id != 6 && x.Id != 7 && x.Id != 8).ToListAsync();
                var refereeList = new List<groupreferee>();

                foreach (var item in cdata)
                {
                    string[] names = { "คณะกรรมการอำนวยการและที่ปรึกษา", "คณะกรรมการดำเนินการ" };
                    string[] duties = { "มีหน้าที่ ให้คำปรึกษา อำนวยความสะดวก กำกับดูแลช่วยเหลือ ประชุม วางแผน ดำเนินงานตามวัตถุประสงค์งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ 72 ปีการศึกษา 2567", "1. ดำเนินการ และประสานงานการจัดการแข่งขัน งานศิลปหัตถกรรมนักเรียน ครั้งที่ 72 ให้บรรลุเป้าหมายตามวัน เวลา และสถานที่ที่กำหนด2. ประสานงานกับผู้เกี่ยวข้องทุกฝ่ายเพื่อการจัดการแข่งขันดำเนินไปด้วยความเรียบร้อย 3. อำนวยความสะดวก ควบคุม กำกับดูแล ช่วยเหลือ ประสานงานติดต่อ จัดหา จัดเตรียม วัสดุอุปกรณ์สถานที่ในการจัดแข่งขัน แก้ไขปัญหาต่างๆ และดูแลการจัดการแข่งขันให้เป็นไปอย่างเรียบร้อย" };

                    for (int i = 0; i < names.Length; i++)
                    {
                        var datagroupreferee = new groupreferee
                        {
                            c_id = item.Id,
                            name = names[i],
                            duty = duties[i],
                            total = 20

                        };

                        refereeList.Add(datagroupreferee);
                    }
                }

                // บันทึกทั้งหมดพร้อมกัน
                _context.groupreferee.AddRange(refereeList);
                await _context.SaveChangesAsync();
            }
            ViewBag.Categories = _context.category.Where(c => c.status == "1" && m_idList.Contains(c.Id)).ToList();
            return View(data.OrderBy(x => x.type));
        }
        [HttpPost]
        public IActionResult Save([FromBody] groupreferee model)
        {
            try
            {
                if (model != null)
                {
                    if (model.name == "")
                    {
                        return Json(new { success = false, message = "กรุณากรอกหัวข้อชื่อคณะกรรมการ" });
                    }
                    if (model.duty == "")
                    {
                        return Json(new { success = false, message = "กรุณากรอกบทบาทหน้าที่" });
                    }
                    if (model.c_id == 0)
                    {
                        return Json(new { success = false, message = "กรุณาเลือกหมวดหมู่" });
                    }
                    if (model.id == 0)
                    {
                        var existingItem = _context.groupreferee
                                        .AsNoTracking().FirstOrDefault(x => x.name == model.name && x.c_id == model.c_id);

                        if (existingItem != null)
                        {
                            // ส่งข้อความแจ้งเตือนกลับในรูปแบบ JSON
                            return Json(new { success = false, message = "หัวข้อชื่อคณะกรรมการนี้มีอยู่ในระบบแล้ว" });
                        }
                        // ถ้าไม่มีข้อมูลซ้ำ ดำเนินการบันทึก
                        _context.groupreferee.Add(model);
                    }
                    else
                    {
                        _context.groupreferee.Update(model);
                    }

                    _context.SaveChanges();
                }
                else
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลก่อน" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
            }

            return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
        }
        [HttpGet]
        public IActionResult GetDetails(int id)
        {
            var item = _context.groupreferee
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.name,
                    x.c_id,
                    x.duty
                })
                .FirstOrDefault();

            if (item != null)
            {
                return Json(new { success = true, data = item });
            }

            return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการ" });
        }
        [HttpPost]
        public IActionResult Deletegroupreferees([FromBody] JsonElement data)
        {
            if (data.TryGetProperty("id", out JsonElement idElement))
            {
                int id = idElement.GetInt32();

                var itemToDelete = _context.groupreferee.FirstOrDefault(x => x.id == id);
                if (itemToDelete != null)
                {
                    _context.Database.BeginTransaction();
                    try
                    {
                        _context.groupreferee.Remove(itemToDelete);
                        _context.SaveChanges();
                        _context.referee.Where(x => x.g_id == id).ExecuteDelete();
                        _context.Database.CommitTransaction();
                        return Json(new { success = true, message = "ลบข้อมูลสำเร็จ" });
                    }
                    catch (Exception ex)
                    {
                        _context.Database.RollbackTransaction();
                        return Json(new { success = false, message = "เกิดข้อผิดพลาดในการลบข้อมูล " + ex.ToString() });
                    }

                }
            }
            return Json(new { success = false, message = "ไม่พบข้อมูลที่ต้องการลบ" });
        }
        public async Task<IActionResult> DownloadFile(int id)
        {
            // ดึงข้อมูลไฟล์จากฐานข้อมูล
            var fileData = await _context.uploadfilepdf.FindAsync(id);
            if (fileData == null)
            {
                return NotFound("ไม่พบข้อมูลไฟล์ที่ต้องการดาวน์โหลด");
            }

            // สร้างเส้นทางไฟล์ใน wwwroot
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads/pdf", fileData.filename);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("ไม่พบไฟล์ในระบบ");
            }

            // ตรวจสอบสิทธิ์ผู้ใช้ (เพิ่มตามความต้องการของคุณ)
            // กำหนด HTTP Header เพื่อป้องกันการแคช

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";

            // ส่งไฟล์กลับไปยังผู้ใช้
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", fileData.filename);
        }
        [HttpPost]
        public async Task<IActionResult> DeletePdfWithReason(int id, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var data = await _context.Registerhead.FindAsync(id);
            if (data != null)
            {
                try
                {
                    // บันทึกเหตุผลใน Log หรือฐานข้อมูล
                    await _context.Database.BeginTransactionAsync();
                    var deleteLog = new deleteregister
                    {
                        c_id = data.c_id,
                        name = reason,
                        u_id = user.Id,
                        lastupdate = DateTime.Now

                    };
                    _context.deleteregister.Add(deleteLog);
                    await _context.Registerhead.Where(x => x.id == id)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.status, "0"));
                    await _context.SaveChangesAsync();
                    await _context.Database.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await _context.Database.RollbackTransactionAsync();
                }
                // ลบไฟล์

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public async Task<IActionResult> SaveTeamAsync([FromBody] AddTeamViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                // ตัวอย่างการบันทึกข้อมูล
                var newTeam = new Registerhead
                {
                    s_id = model.schoolId,
                    c_id = model.c_id,
                    score = 0,
                    rank = 0,
                    award = "",
                    u_id = user.Id,
                    status = "1",
                    lastupdate = DateTime.Now
                };

                _context.Registerhead.Add(newTeam);
                _context.SaveChanges();
                return Json(new { success = true, message = "บันทึกสำเร็จ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.ToString() });
            }
        }
        public async Task<IActionResult> frmlistreferee()
        {
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            var data = await _context.referee.Where(x => x.status == "1" && m_idList.Contains(x.m_id))
            .Include(x => x.Competitionlist)
            .AsNoTracking()
            .ToListAsync();
            return View(data.OrderBy(x => x.id));
        }
        public async Task<IActionResult> frmprintresult(int c_id)
        {
            var user = await _userManager.GetUserAsync(User);
             List<int> m_idList = user.m_id.Split(',')
                         .Select(int.Parse)
                         .ToList();
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1"&&m_idList.Contains(x.Id)).ToList(), "Id", "Name");
            ViewBag.currentTypelevel = c_id;


            return View();
        }
    }
}