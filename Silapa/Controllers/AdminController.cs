using System;
using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;
using Syncfusion.EJ2.Linq;
using OfficeOpenXml;
using iText.Kernel.Pdf.Filters; // Import EPPlus
namespace Silapa.Controllers
{
    [Authorize]
    public class AdminController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        public AdminController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
        }
        public ActionResult frmlist(string c_id)
        {
            int a = Convert.ToInt32(c_id);
            var data = _context.Competitionlist.Where(x => x.status == "1").ToList();
            if (c_id != "" || c_id != null)
            {
                data = data.Where(x => x.c_id == a).ToList();
            }
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = a;

            return View(data.OrderBy(x => x.Id));
        }
        [HttpGet]
        public ActionResult frmListAdd(int Id)
        {
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = 0;
            if (Id == 0)
                return View(new Competitionlist());
            else
                return View(_context.Competitionlist.Include(x => x.dCompetitionlists).Where(x => x.Id == Id).FirstOrDefault());
        }
        [HttpPost]
        // [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCompetition([FromBody] CompetitionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {

                int headId = 0;
                try
                {
                    // แปลงข้อมูลจาก ViewModel ไปยังโมเดล Competitionlist
                    var competition = new Competitionlist
                    {
                        Id = viewModel.Competition.Id,
                        c_id = viewModel.Competition.c_id,
                        Name = viewModel.Competition.Name,
                        type = viewModel.Competition.Type,
                        teacher = viewModel.Competition.Teacher,
                        student = viewModel.Competition.Student,
                        director = viewModel.Competition.Director,
                        details = viewModel.Competition.Details,
                        status = viewModel.Competition.Status,
                        lastupdate = viewModel.Competition.LastUpdate,
                        // ไม่ต้องตั้งค่า dCompetitionlists ที่นี่ เนื่องจากเราจะเพิ่มในขั้นตอนถัดไป
                    };


                    var user = await _userManager.GetUserAsync(User);
                    await _context.Database.BeginTransactionAsync();


                    if (competition.Id == 0)
                    {
                        var data = await _context.Competitionlist.AsNoTracking().Where(x => x.Name == competition.Name).FirstOrDefaultAsync();
                        if (data != null)
                        {
                            return Json(new { success = false, message = "ชื่อรายการ" + competition.Name + " มีการบันทึกข้อมูลแล้วกรุณาตรวจสอบ" });
                        }
                        _context.Competitionlist.Add(competition);
                        await _context.SaveChangesAsync(); // บันทึกเพื่อให้ได้ค่า id ของ Registerhead
                                                           // ใช้ id ของ Registerhead ที่บันทึกแล้วสำหรับ Registerdetail
                        headId = competition.Id;
                    }
                    else
                    {
                        headId = competition.Id;
                        _context.Competitionlist.Update(competition);
                        await _context.dCompetitionlist.Where(x => x.h_id == headId).ExecuteDeleteAsync();
                    }

                    int nos = 1;
                    foreach (var dr in viewModel.DCompetition)
                    {
                        var newData = new dCompetitionlist
                        {
                            id = nos,
                            h_id = headId,
                            name = dr.Name,
                            scrol = dr.Scrol,
                            u_id = user.Id,

                            lastupdate = DateTime.Now
                        };
                        try
                        {
                            await _context.dCompetitionlist.AddRangeAsync(newData);
                        }
                        catch (Exception ex)
                        {
                            return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" + nos + ex.ToString() });
                        }

                        nos += 1;
                    }
                    await _context.SaveChangesAsync();
                    await _context.Database.CommitTransactionAsync();
                    return Json(new
                    {
                        success = true,
                        message = "บันทึกข้อมูลสำเร็จ",
                        redirectUrl = Url.Action("frmList", "Admin", new { c_id = viewModel.Competition.c_id }) // Generate the redirect URL
                    });
                }
                catch (Exception ex)
                {
                    // จัดการกับข้อผิดพลาดและคืนค่าแจ้งเตือน
                    await _context.Database.RollbackTransactionAsync();
                    return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" + ex.ToString() });
                }
            }
            else
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" + viewModel.DCompetition });
            }

        }
        public async Task<IActionResult> frmListDel(int? id, int c_id)
        {
            await _context.Competitionlist.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(i => i.status, "0")
            .SetProperty(i => i.lastupdate, DateTime.Now)
            );
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = c_id;
            return RedirectToAction("frmList", new { c_id = c_id.ToString() });
        }
        public ActionResult frmschoolList(string g_id)
        {
            int a = Convert.ToInt32(g_id);
            var data = _context.school.AsNoTracking().Where(x => x.status == "1").Include(x => x.Affiliation).Include(x => x.grouplist).ToList();

            if (a != 0)
            {
                data = data.Where(x => x.g_id == a).ToList();
            }
            IList<ApplicationUser> users = _userManager.Users.ToList();
            ViewBag.users = users;
            ViewBag.levelData = new SelectList(_context.grouplist.ToList(), "Id", "Name");
            ViewBag.currentTypelevel = a;

            return View(data.OrderBy(x => x.Id));
        }
        [HttpGet]
        public ActionResult frmschoolAdd(int Id)
        {
            ViewBag.levelData = new SelectList(_context.grouplist.ToList(), "Id", "Name");
            ViewBag.currentTypelevel = 0;
            ViewBag.levelData1 = new SelectList(_context.Affiliation.ToList(), "Id", "Name");
            ViewBag.currentTypelevel1 = 0;
            if (Id == 0)
                return View(new school());
            else
                return View(_context.school.Find(Id));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> frmschoolAdd([Bind("Id,Name,titlename,FirstName,LastName,tel,g_id,a_id")] school data)
        {
            if (!ModelState.IsValid)
            {
                await _context.Database.BeginTransactionAsync();
                data.status = "1";
                data.lastupdate = DateTime.Now;
                if (data.Id == 0)
                {
                    try
                    {
                        var cs = await _context.school.Where(x => x.Name == data.Name).FirstOrDefaultAsync();
                        if (cs != null)
                        {
                            ModelState.AddModelError(string.Empty, "ชื่อโรงเรียนนี้มีการบันทึกข้อมูลแล้วกรุณาตรวจสอบ");
                            goto exit;
                        }
                        await _context.school.AddAsync(data);
                        await _context.SaveChangesAsync();
                        int newId = data.Id; // ใช้ property ของ data เพื่อเข้าถึงค่า id หลังจากการบันทึก
                        string username = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
                        string email = $"{username}@example.com";
                        string password = "Kr@123";

                        var user = new ApplicationUser();
                        user.UserName = username;
                        user.Email = email;
                        // user.EmailConfirmed= false;

                        user.titlename = data.titlename;
                        user.FirstName = data.FirstName;
                        user.LastName = data.LastName;
                        user.s_id = newId;//Convert.ToInt32(lastRecord.Id) + 1;

                        await _userManager.CreateAsync(user, password);
                        await _userManager.AddToRoleAsync(user, "Member");
                        await _context.Database.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _context.Database.RollbackTransactionAsync();
                    }
                }
                else
                {
                    var user = _userManager.Users.FirstOrDefault(u => u.s_id == data.Id);
                    user.titlename = data.titlename;
                    user.FirstName = data.FirstName;
                    user.LastName = data.LastName;
                    await _userManager.UpdateAsync(user);

                    await _context.school.Where(x => x.Id == data.Id).ExecuteUpdateAsync(
                        x => x.SetProperty(x => x.Name, data.Name)
                        .SetProperty(x => x.titlename, data.titlename)
                        .SetProperty(x => x.FirstName, data.FirstName)
                        .SetProperty(x => x.LastName, data.LastName)
                        .SetProperty(x => x.tel, data.tel)
                        .SetProperty(x => x.g_id, data.g_id)
                        .SetProperty(x => x.a_id, data.a_id)
                        .SetProperty(x => x.lastupdate, data.lastupdate)
                    );
                    await _context.SaveChangesAsync();
                    await _context.Database.CommitTransactionAsync();
                }
                if (User.IsInRole("Member"))
                {
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("frmschoolList", new { g_id = data.g_id.ToString() });
                //return View(data);
            }
        exit:;
            ViewBag.levelData = new SelectList(_context.grouplist.ToList(), "Id", "Name");
            ViewBag.currentTypelevel = data.g_id;
            ViewBag.levelData1 = new SelectList(_context.Affiliation.ToList(), "Id", "Name");
            ViewBag.currentTypelevel1 = data.a_id;

            return View(data);
        }
        public async Task<IActionResult> frmschoolDel(int? id, string userId)
        {
            await _context.school.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(i => i.status, "0")
            .SetProperty(i => i.lastupdate, DateTime.Now)
            );
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }
            // ล็อคผู้ใช้โดยไม่มีกำหนด (ตั้งค่า LockoutEnd เป็น MaxValue)
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = 0;
            return RedirectToAction("frmschoolList");
        }
        public async Task<IActionResult> frmschoolreset(string userId)
        {
            string newPassword = "Kr@123";
            var user = await _userManager.FindByIdAsync(userId);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = 0;
            return RedirectToAction("frmschoolList");
        }
        public IActionResult frmsetupsystem(int id)
        {
            if (id == 0)
            {
                return View(new setupsystem());
            }
            else
            {
                return View(_context.setupsystem.Find(id));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> frmsetupsystem(setupsystem model)
        {
            ModelState.Remove("u_id");
            ModelState.Remove("lastupdate");
            ModelState.Remove("certificate");
            ModelState.Remove("cardstudents");
            ModelState.Remove("carddirector");
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid)
            {
                model.u_id = user.Id;
                model.lastupdate = DateTime.Now;
                ////อัพโหลดไฟล์
                ///
                var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/card");

                // สร้างโฟลเดอร์ถ้ายังไม่มี
                if (!Directory.Exists(uploadDirectory))
                    Directory.CreateDirectory(uploadDirectory);
                // ตรวจสอบและบันทึกไฟล์
                // ตรวจสอบและบันทึกโลโก้
                if (model.Logo != null)
                {
                    var fileName = Path.GetFileName(model.Logo.FileName);
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Logo.CopyToAsync(stream);
                    }

                    model.LogoPath = $"/card/{fileName}";
                }

                // ตรวจสอบและบันทึกไฟล์
                if (model.Certificate != null && model.Certificate.Length <= 10 * 1024 * 1024)
                {
                    var filename = "certificate.png";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Certificate.CopyToAsync(stream);
                    }
                    model.certificate = filename;
                }

                if (model.CardStudents != null && model.CardStudents.Length <= 10 * 1024 * 1024)
                {
                    var filename = "cardstudents.pdf";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardStudents.CopyToAsync(stream);
                    }
                    model.cardstudents = filename;
                }

                if (model.CardTeacher != null && model.CardTeacher.Length <= 10 * 1024 * 1024)
                {
                    var filename = "cardteacher.pdf";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardTeacher.CopyToAsync(stream);
                    }
                    model.cardteacher = filename;
                }

                if (model.CardDirector != null && model.CardDirector.Length <= 10 * 1024 * 1024)
                {
                    var filename = "carddirector.pdf";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardDirector.CopyToAsync(stream);
                    }
                    model.carddirector = filename;
                }

                if (model.CardReferee != null && model.CardReferee.Length <= 10 * 1024 * 1024)
                {
                    var filename = "cardreferee.pdf";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardReferee.CopyToAsync(stream);
                    }
                    model.cardreferee = filename;
                }
                if (model.id == 0)
                {
                    _context.setupsystem.Add(model);
                }
                else
                {
                    _context.setupsystem.Update(model);
                }
                _context.SaveChanges();
                Notify("แก้ไขข้อมูลเรียบร้อยแล้ว", "Update Success", NotificationType.success);
            }
            return View(model);
        }
        public async Task<IActionResult> frmschoolexcel()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> frmschoolexcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("กรุณาเลือกไฟล์ Excel ที่ต้องการอัปโหลด");
            }
            var sqlschool = await _context.school.AsNoTracking().Where(x => x.status == "1").ToListAsync();
            var records = new List<school>();
            using (var stream = new MemoryStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // ตั้งค่าการใช้งานให้เป็น NonCommercial
                await file.CopyToAsync(stream);

                using (var package = new ExcelPackage(stream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets["Sheet1"];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Assuming the first row is header
                    {
                        var record = new school
                        {
                            Name = worksheet.Cells[row, 1].Value?.ToString(),
                            g_id = Convert.ToInt16(worksheet.Cells[row, 2].Value?.ToString()),
                            a_id = 0,
                            titlename = "-",
                            FirstName = worksheet.Cells[row, 1].Value?.ToString(),
                            LastName = "",
                            tel = "-",
                            status = "1",
                            lastupdate = DateTime.Now
                        };

                        // ตรวจสอบว่ามีโรงเรียนนี้อยู่แล้วหรือไม่
                        var checkSchool = sqlschool.Where(x => x.Name == record.Name).FirstOrDefault();
                        if (checkSchool == null)
                        {
                            records.Add(record);

                            // บันทึกข้อมูลโรงเรียน
                            await _context.school.AddAsync(record);
                            await _context.SaveChangesAsync();

                            // สร้างผู้ใช้ใหม่
                            string username = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";
                            string email = $"{username}@example.com";
                            string password = "Kr@123";

                            var user = new ApplicationUser
                            {
                                UserName = username,
                                Email = email,
                                titlename = record.titlename,
                                FirstName = record.FirstName,
                                LastName = record.LastName,
                                s_id = record.Id // ใช้ Id ของ school ที่เพิ่งบันทึก
                            };

                            // สร้างผู้ใช้และกำหนด role
                            var userCreationResult = await _userManager.CreateAsync(user, password);
                            if (userCreationResult.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(user, "Member");
                            }
                            else
                            {
                                // ถ้าไม่สามารถสร้างผู้ใช้ได้ ให้ยกเลิกและลบโรงเรียนที่เพิ่มไว้
                                _context.school.Remove(record);
                                await _context.SaveChangesAsync();
                                throw new Exception("Error creating user: " + string.Join(", ", userCreationResult.Errors.Select(e => e.Description)));
                            }
                        }
                    }
                }
            }

            // Process the data, such as saving it to the database
            // _context.YourDbSet.AddRange(records);
            // await _context.SaveChangesAsync();
            return RedirectToAction("frmschoolList");
        }
        public async Task<IActionResult> frmcontacts(IFormFile file)
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";
            var data = await _context.contacts.Where(x => x.status == "1").ToListAsync();
            return View(data.OrderBy(x => x.id));
        }
        [HttpPost]
        public async Task<IActionResult> saveContactModal([FromBody] contacts contactData)
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid)
            {
                contactData.u_id = user.Id;
                if (contactData.id == 0)
                {
                    _context.contacts.Add(contactData);
                }
                else
                {
                    _context.contacts.Update(contactData);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            return BadRequest(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateImage(string croppedImage, int id)
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";
            // Process the croppedImage data, e.g., convert to byte array and save to server
            var newImageUrl = await SaveCroppedImage(croppedImage, id); // Method to save image and return URL

            // Update the database record with the new image URL
            var record = await _context.contacts.Where(x => x.id == id).FirstOrDefaultAsync();
            if (record != null)
            {
                //  record.ImageUrl = newImageUrl;
                ///await _context.SaveChangesAsync();
                ///
                await _context.contacts.Where(x => x.id == id)
                .ExecuteUpdateAsync(x => x.SetProperty(x => x.ImageUrl, newImageUrl));
            }
            return Json(new { success = true, newImageUrl });
        }
        private async Task<string> SaveCroppedImage(string base64Image, int h_id)
        {
            var imageBytes = Convert.FromBase64String(base64Image.Split(',')[1]);
            var fileName = $"image_{Guid.NewGuid().ToString()}_{h_id}.jpg";
            var filePath = Path.Combine("wwwroot/images/contacts", fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/images/contacts/{fileName}"; // Return the URL to be saved in the database
        }
        public async Task<IActionResult> frmlistregisterAll(int c_id)
        {

            var levelData = _context.Affiliation
    .Select(g => new SelectListItem
    {
        Value = g.Id.ToString(),
        Text = g.Name
    })
    .ToList();

            ViewBag.levelData = levelData;
            ViewBag.currentTypelevel = c_id; // ค่าเริ่มต้นที่ต้องการเลือก

            /*  var query =  _context.Registerhead
              .AsNoTracking()
       .Where(x => x.status == "1" && x.School.a_id == c_id)
       .Include(x => x.Registerdetail) // โหลด Registerdetail
       .Include(x => x.School) // โหลด School
           .ThenInclude(s => s.Affiliation) // โหลด Affiliation ผ่าน School
       .Include(x => x.Competitionlist) // โหลด Competitionlist
           .ThenInclude(c => c.racedetails) // โหลด Racedetails ผ่าน Competitionlist
           .ThenInclude(r => r.Racelocation); // โหลด Racelocation ผ่าน Racedetails
              var data = await query.OrderBy(x => x.id).ToListAsync();

            //var data = await query.OrderBy(x => x.id).ToListAsync();
            return View(data);*/
            var query = await _context.Registerhead
    .Where(x => x.status != "0" && x.School.a_id == c_id)
    .Include(x => x.School)
    .Include(x => x.Competitionlist)
    .Include(x => x.Registerdetail)
    .AsNoTracking()
    .Select(x => new
    {
        Id = x.id,
        SchoolName = x.School.Name,
        CompetitionName = x.Competitionlist.Name,
        Award = x.award,
        Rank = x.rank, // Retrieve raw rank value
        Students = x.Registerdetail.Where(rd => rd.Type == "student").Select(rd => new PersonViewModel2
        {
            No = rd.no,
            Prefix = rd.Prefix,
            FirstName = rd.FirstName,
            LastName = rd.LastName
        }).ToList(),
        Teachers = x.Registerdetail.Where(rd => rd.Type == "teacher").Select(rd => new PersonViewModel2
        {
            No = rd.no,
            Prefix = rd.Prefix,
            FirstName = rd.FirstName,
            LastName = rd.LastName
        }).ToList()
    })
    .ToListAsync(); // Fetch data into memory

            // Transform Rank in memory
            var data = query.Select(x => new RegisterViewModel2
            {
                Id = x.Id,
                SchoolName = x.SchoolName,
                CompetitionName = x.CompetitionName,
                Award = x.Award,
                Rank = x.Rank switch
                {
                    1 => "ชนะเลิศ",
                    2 => "รองชนะเลิศ อันดับ 1",
                    3 => "รองชนะเลิศ อันดับ 2",
                    null => "ไม่มีอันดับ", // Handle null case
                    _ => $"อันดับ {x.Rank}" // Handle other ranks
                },
                Students = x.Students,
                Teachers = x.Teachers
            })
            .OrderBy(x=>x.Rank)
            .ToList();

            return View(data);
        }
        public async Task<IActionResult> frmfileList()
        {
            var data = await _context.filelist.Where(x => x.status == "1").ToListAsync();
            return View(data);
        }
        [HttpGet]
        public ActionResult frmfileListAdd(int Id)
        {
            if (Id == 0)
                return View(new filelist());
            else
                return View(_context.filelist.Where(x => x.id == Id).FirstOrDefault());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCompetitionFilelist(filelist model)
        {
            if (ModelState.IsValid)
            {
                if (model.File != null)
                {
                    // ตรวจสอบขนาดไฟล์ (ไม่เกิน 10 MB)
                    if (model.File.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("File", "ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
                        return View(model);
                    }

                    // ตรวจสอบและดึงนามสกุลไฟล์
                    string extension = Path.GetExtension(model.File.FileName); // ตัวอย่าง .pdf, .jpg, etc.

                    // สร้างชื่อไฟล์ใหม่
                    string newFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/filelist", newFileName);

                    // สร้างโฟลเดอร์หากยังไม่มี
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    // บันทึกไฟล์
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                    model.fileurl = "/uploads/filelist/" + newFileName; // หรือฟิลด์อื่นตามต้องการ
                }
                model.status = "1";
                model.lastupdate = DateTime.Now;
                if (model.id == 0)
                {
                    // บันทึกข้อมูลในฐานข้อมูล (ถ้าจำเป็น)
                    _context.filelist.Add(model);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.filelist.Where(x => x.id == model.id)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.name, model.name)
                    .SetProperty(x => x.fileurl, model.fileurl)
                    .SetProperty(x => x.status, model.status)
                    .SetProperty(x => x.lastupdate, DateTime.Now)
                    );
                }
                return RedirectToAction("frmfileList", "Admin");
            }
            return View(model);
        }
        public IActionResult DownloadFilelist(int id)
        {
            // ค้นหาไฟล์จากฐานข้อมูลโดยใช้ id
            var file = _context.filelist.FirstOrDefault(f => f.id == id);

            if (file == null || string.IsNullOrEmpty(file.fileurl))
            {
                return NotFound("ไฟล์ที่คุณต้องการดาวน์โหลดไม่มีอยู่");
            }

            // ระบุเส้นทางของไฟล์ใน wwwroot/uploads
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.fileurl.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("ไฟล์ที่คุณต้องการดาวน์โหลดไม่พบ");
            }

            // คืนค่าไฟล์ให้ดาวน์โหลด
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream"; // หรือปรับเปลี่ยนให้เหมาะสมกับประเภทไฟล์

            return File(fileBytes, contentType, file.fileurl);
        }
        [HttpPost]
        public IActionResult DeleteFilelist(int id)
        {
            // ค้นหาไฟล์จากฐานข้อมูล
            var file = _context.filelist.FirstOrDefault(f => f.id == id);

            if (file == null)
            {
                return NotFound("ไม่พบข้อมูลที่ต้องการลบ");
            }

            // ลบไฟล์จาก wwwroot (ถ้ามีไฟล์อยู่)
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.fileurl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath); // ลบไฟล์จากโฟลเดอร์
            }

            // ลบเรคอร์ดในฐานข้อมูล
            _context.filelist.Remove(file);
            _context.SaveChanges();

            return RedirectToAction("frmfileList", "Admin");
        }
    }
}