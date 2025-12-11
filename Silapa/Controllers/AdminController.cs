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
using iText.Kernel.Pdf.Filters;
namespace Silapa.Controllers
{
    [Authorize]
    public class AdminController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        public AdminController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _contextFactory = contextFactory;
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
        public ActionResult frmschoolList(string g_id, string a_id)
        {
            // 2. (แก้ไข) แปลงค่า g_id และ a_id (แบบปลอดภัย)
            int g_val = 0;
            int a_val = 0;
            int.TryParse(g_id, out g_val);
            int.TryParse(a_id, out a_val);

            var data = _context.school.AsNoTracking()
                               .Where(x => x.status == "1")
                               .Include(x => x.Affiliation)
                               .Include(x => x.grouplist).ToList();

            // 3. (เดิม) กรองตาม g_id
            if (g_val != 0)
            {
                data = data.Where(x => x.g_id == g_val).ToList();
            }

            // 4. (เพิ่ม) กรองตาม a_id (สังกัด)
            if (a_val != 0)
            {
                data = data.Where(x => x.a_id == a_val).ToList();
            }

            IList<ApplicationUser> users = _userManager.Users.ToList();
            ViewBag.users = users;

            // 5. (แก้ไข) ส่ง SelectList ที่มี "ค่าที่ถูกเลือก" กลับไป
            ViewBag.levelData = new SelectList(_context.grouplist.ToList(), "Id", "Name", g_val);
            ViewBag.currentTypelevel = g_val; // (บรรทัดนี้ไม่จำเป็นแล้ว แต่เก็บไว้ได้)

            // 6. (เพิ่ม) ส่งข้อมูล "สังกัด" (Affiliation) ไปที่ View
            ViewBag.affiliationData = new SelectList(_context.Affiliation.ToList(), "Id", "Name", a_val);
            ViewBag.currentAffiliation = a_val; // (บรรทัดนี้ไม่จำเป็นแล้ว แต่เก็บไว้ได้)

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
        public async Task<IActionResult> frmsetupsystem(int? id)
        {
            setupsystem systemSetup;
            if (id.HasValue && id.Value != 0)
            {
                // 2. ถ้ามี ID ส่งมา (เช่น /2) ให้ดึงข้อมูลตาม ID นั้น
                systemSetup = await _context.setupsystem
                                        .Include(s => s.TimelineEvents.OrderBy(t => t.DisplayOrder))
                                        .FirstOrDefaultAsync(s => s.id == id.Value); // <-- จะได้ id = 2
            }
            else
            {
                systemSetup = await _context.setupsystem
                                   .Include(s => s.TimelineEvents.OrderBy(t => t.DisplayOrder))
                                   .FirstOrDefaultAsync(s => s.status == "1");

                if (systemSetup == null)
                {
                    // กรณีไม่เจอข้อมูล ให้สร้างใหม่ หรือกลับไปหน้า Index
                    return NotFound(); // หรือ return View(new setupsystem());
                }

            }
            return View(systemSetup);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> frmsetupsystem(setupsystem model)
        {
            // 1. Validate Model State
            ModelState.Remove("u_id");
            ModelState.Remove("lastupdate");
            // ลบ IFormFile properties ออกจาก Validation เพราะมันอาจจะไม่ได้ถูกส่งมา (เช่นตอนไม่เปลี่ยนไฟล์)
            ModelState.Remove("Logo");
            ModelState.Remove("Certificate");
            ModelState.Remove("CardStudents");
            ModelState.Remove("CardTeacher");
            ModelState.Remove("CardDirector");
            ModelState.Remove("CardReferee");
            ModelState.Remove("DeclarationFile");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // ไม่ได้ login
            }

            // 2. เตรียมโฟลเดอร์สำหรับอัปโหลด
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/card");
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            // 3. ตรวจสอบว่าเป็น "การสร้างใหม่" หรือ "การอัปเดต"
            if (model.id == 0)
            {
                // ----- นี่คือส่วนของการ "สร้างใหม่" (Create) -----

                // กำหนดค่าเริ่มต้น
                model.u_id = user.Id;
                model.lastupdate = DateTime.Now;

                // --- ประมวลผลไฟล์ที่อัปโหลด (เหมือนเดิม) ---
                if (model.Logo != null)
                {
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.Logo.FileName)}"; // สร้างชื่อไฟล์ใหม่กันซ้ำ
                    var filePath = Path.Combine(uploadDirectory, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Logo.CopyToAsync(stream);
                    }
                    model.LogoPath = $"/card/{fileName}"; // อัปเดต path ใน model
                }

                if (model.Certificate != null)
                {
                    var fileExtension = Path.GetExtension(model.Certificate.FileName);
                    var newFileName = $"cert_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Certificate.CopyToAsync(stream);
                    }
                    model.certificate = newFileName; // ⚡️ ใช้ชื่อใหม่
                }

                if (model.CardStudents != null)
                {
                    var fileExtension = Path.GetExtension(model.CardStudents.FileName);
                    var newFileName = $"std_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardStudents.CopyToAsync(stream);
                    }
                    model.cardstudents = newFileName; // ⚡️ ใช้ชื่อใหม่
                }

                if (model.CardTeacher != null)
                {
                    var fileExtension = Path.GetExtension(model.CardTeacher.FileName);
                    var newFileName = $"tch_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardTeacher.CopyToAsync(stream);
                    }
                    model.cardteacher = newFileName; // ⚡️ ใช้ชื่อใหม่
                }

                if (model.CardDirector != null)
                {
                    var fileExtension = Path.GetExtension(model.CardDirector.FileName);
                    var newFileName = $"dir_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardDirector.CopyToAsync(stream);
                    }
                    model.carddirector = newFileName; // ⚡️ ใช้ชื่อใหม่
                }

                if (model.CardReferee != null)
                {
                    var fileExtension = Path.GetExtension(model.CardReferee.FileName);
                    var newFileName = $"ref_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardReferee.CopyToAsync(stream);
                    }
                    model.cardreferee = newFileName; // ⚡️ ใช้ชื่อใหม่
                }
                if (model.DeclarationFile != null)
                {
                    var fileExtension = Path.GetExtension(model.DeclarationFile.FileName);
                    var newFileName = $"{Guid.NewGuid()}{fileExtension}";

                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.DeclarationFile.CopyToAsync(stream);
                    }
                    model.DeclarationFilePath = newFileName;
                }
                // --- จบการประมวลผลไฟล์ ---

                // ตรวจสอบ ModelState อีกครั้งหลังจากตั้งค่า server-side
                if (!ModelState.IsValid)
                {
                    // ถ้าไม่ผ่าน ก็ต้องส่งข้อมูล Timeline กลับไป
                    model.TimelineEvents = new List<TimelineItem>(); // ส่ง List ว่างๆ กลับไป
                    return View(model);
                }

                _context.setupsystem.Add(model);
                Notify("บันทึกข้อมูลใหม่เรียบร้อยแล้ว", "Create Success", NotificationType.success);
            }
            else
            {
                // ----- นี่คือส่วนของการ "อัปเดต" (Update) - นี่คือส่วนที่แก้ไข -----

                // 1. ดึงข้อมูล *เก่า* จากฐานข้อมูลมาก่อน
                var entityInDb = await _context.setupsystem.FindAsync(model.id);

                if (entityInDb == null)
                {
                    return NotFound();
                }

                // 2. อัปเดตค่าพื้นฐาน (Text, Date, etc.) จาก model ที่ส่งมา
                entityInDb.name = model.name;
                entityInDb.nameagency = model.nameagency;
                entityInDb.time = model.time;
                entityInDb.yaer = model.yaer;
                entityInDb.ProvinceName = model.ProvinceName;
                entityInDb.racedate = model.racedate;
                entityInDb.startregisterdate = model.startregisterdate;
                entityInDb.endgisterdate = model.endgisterdate;
                entityInDb.startedit = model.startedit;
                entityInDb.endedit = model.endedit;
                entityInDb.certificatedate = model.certificatedate;
                entityInDb.Coordinator = model.Coordinator;
                entityInDb.token = model.token;
                entityInDb.status = model.status; // อัปเดต status จาก hidden field

                entityInDb.SloganThai = model.SloganThai;
                entityInDb.SloganEnglish = model.SloganEnglish;
                entityInDb.HeroVideoPath = model.HeroVideoPath;
                entityInDb.AboutHeading = model.AboutHeading;
                entityInDb.AboutText1 = model.AboutText1;
                entityInDb.AboutText2 = model.AboutText2;

                // 3. อัปเดตค่า Server-side
                entityInDb.u_id = user.Id;
                entityInDb.lastupdate = DateTime.Now;

                // 4. ประมวลผลไฟล์:
                //    ตรวจสอบว่ามีการ *อัปโหลดไฟล์ใหม่* หรือไม่
                //    ถ้ามี (model.Logo != null) -> ให้บันทึกไฟล์ใหม่ และอัปเดต 'entityInDb.LogoPath'
                //    ถ้าไม่มี (model.Logo == null) -> เรา "ไม่ต้องทำอะไร" 'entityInDb.LogoPath' จะยังคงเป็นค่าเก่าที่ถูกต้อง

                if (model.Logo != null)
                {
                    // (แนะนำ: ควรลบไฟล์โลโก้เก่า 'entityInDb.LogoPath' ออกจากเซิร์ฟเวอร์ก่อน)
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.Logo.FileName)}"; // สร้างชื่อไฟล์ใหม่กันซ้ำ
                    var filePath = Path.Combine(uploadDirectory, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Logo.CopyToAsync(stream);
                    }
                    entityInDb.LogoPath = $"/card/{fileName}"; // อัปเดต path ใน entity ที่ดึงจาก DB
                }

                // ทำเหมือนกันสำหรับไฟล์อื่นๆ ทั้งหมด
                if (model.Certificate != null)
                {
                    var filename = "certificate.png";
                    var filePath = Path.Combine(uploadDirectory, filename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Certificate.CopyToAsync(stream);
                    }
                    entityInDb.certificate = filename;
                }

                if (model.CardStudents != null)
                {
                    // A. ลบไฟล์เก่า
                    if (!string.IsNullOrEmpty(entityInDb.cardstudents))
                    {
                        var oldFilePath = Path.Combine(uploadDirectory, entityInDb.cardstudents);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // B. บันทึกไฟล์ใหม่
                    var fileExtension = Path.GetExtension(model.CardStudents.FileName);
                    var newFileName = $"std_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardStudents.CopyToAsync(stream);
                    }
                    entityInDb.cardstudents = newFileName; // ⚡️ อัปเดตชื่อใหม่
                }

                if (model.CardTeacher != null)
                {
                    // A. ลบไฟล์เก่า
                    if (!string.IsNullOrEmpty(entityInDb.cardteacher))
                    {
                        var oldFilePath = Path.Combine(uploadDirectory, entityInDb.cardteacher);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // B. บันทึกไฟล์ใหม่
                    var fileExtension = Path.GetExtension(model.CardTeacher.FileName);
                    var newFileName = $"tch_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardTeacher.CopyToAsync(stream);
                    }
                    entityInDb.cardteacher = newFileName; // ⚡️ อัปเดตชื่อใหม่
                }

                if (model.CardDirector != null)
                {
                    // A. ลบไฟล์เก่า
                    if (!string.IsNullOrEmpty(entityInDb.carddirector))
                    {
                        var oldFilePath = Path.Combine(uploadDirectory, entityInDb.carddirector);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // B. บันทึกไฟล์ใหม่
                    var fileExtension = Path.GetExtension(model.CardDirector.FileName);
                    var newFileName = $"cdt_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardDirector.CopyToAsync(stream);
                    }
                    entityInDb.carddirector = newFileName; // ⚡️ อัปเดตชื่อใหม่
                }

                if (model.CardReferee != null)
                {
                    // A. ลบไฟล์เก่า
                    if (!string.IsNullOrEmpty(entityInDb.cardreferee))
                    {
                        var oldFilePath = Path.Combine(uploadDirectory, entityInDb.cardreferee);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // B. บันทึกไฟล์ใหม่
                    var fileExtension = Path.GetExtension(model.CardReferee.FileName);
                    var newFileName = $"crf_{Guid.NewGuid()}{fileExtension}"; // ⚡️ NEW
                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CardReferee.CopyToAsync(stream);
                    }
                    entityInDb.cardreferee = newFileName; // ⚡️ อัปเดตชื่อใหม่
                }
                if (model.DeclarationFile != null)
                {
                    // 1. (แนะนำ) ลบไฟล์เก่าทิ้งก่อน (ถ้ามี)
                    if (!string.IsNullOrEmpty(entityInDb.DeclarationFilePath))
                    {
                        var oldFilePath = Path.Combine(uploadDirectory, entityInDb.DeclarationFilePath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // 2. (ใหม่) สร้างชื่อไฟล์ที่ไม่ซ้ำกัน
                    var fileExtension = Path.GetExtension(model.DeclarationFile.FileName);
                    var newFileName = $"{Guid.NewGuid()}{fileExtension}";

                    var filePath = Path.Combine(uploadDirectory, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.DeclarationFile.CopyToAsync(stream);
                    }
                    entityInDb.DeclarationFilePath = newFileName; // 3. (ใหม่) บันทึกชื่อใหม่
                }
                // 5. ไม่จำเป็นต้องเรียก _context.setupsystem.Update(entityInDb)
                //    เพราะ EF กำลังติดตาม (Track) 'entityInDb' ที่เรา FindAsync มา
                //    มันรู้ว่าเราแก้ไขอะไรไปบ้างแล้ว
                _context.Entry(entityInDb).State = EntityState.Modified;
                Notify("แก้ไขข้อมูลเรียบร้อยแล้ว", "Update Success", NotificationType.success);
            }

            // ----- บันทึกการเปลี่ยนแปลงลง DB (ทั้ง Create และ Update) -----
            try
            {
                int recordsAffected = await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // จัดการ Error (ถ้ามี)
                Notify("เกิดข้อผิดพลาดในการบันทึก: " + ex.Message, "Error", NotificationType.error);

                // ถ้า Error, ต้องส่ง Timeline กลับไปที่ View ด้วย
                model.TimelineEvents = await _context.TimelineItem
                                          .Where(t => t.SetupSystemID == model.id)
                                          .OrderBy(t => t.DisplayOrder)
                                          .ToListAsync();
                return View(model); // ถ้าล้มเหลว, กลับไปที่ฟอร์มเดิมพร้อม Error
            }
            return RedirectToAction("frmsetupsystem", new { id = model.id });
        }
        public IActionResult TimelineEvents_Create(int setupId)
        {
            var newTimelineItem = new TimelineItem { SetupSystemID = setupId };
            // **สำคัญ:** คืนค่าเป็น PartialView แทน View
            return PartialView("_TimelineItemForm", newTimelineItem);
        }
        // POST: Admin/CreateTimeline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimeline(TimelineItem timelineItem)
        {
            // --- เพิ่มโค้ดส่วนนี้เพื่อ Debug ---
            if (!ModelState.IsValid)
            {
                // วนลูปเพื่อดูว่ามี Error อะไรบ้าง
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                // ให้ใส่ Breakpoint ที่บรรทัดนี้แล้วดูค่า errors ใน Debugger
                // คุณจะเห็นข้อความบอกว่า Field ไหนมีปัญหาและเพราะอะไร
            }
            // --- จบส่วน Debug ---
            // ตรวจสอบข้อมูลจากฟอร์ม
            if (ModelState.IsValid)
            {
                _context.Add(timelineItem);
                await _context.SaveChangesAsync();
                // ส่ง JSON กลับไปบอก JavaScript ว่าบันทึกสำเร็จ
                return Json(new { success = true });
            }
            // ถ้าข้อมูลไม่ถูกต้อง ให้ส่งฟอร์มพร้อม Error กลับไปแสดงใน Modal เดิม
            return PartialView("_TimelineItemForm", timelineItem);
        }


        // GET: Admin/EditTimeline/1
        // คืนค่า Partial View ที่เป็นฟอร์มพร้อมข้อมูลเดิมสำหรับแก้ไข
        public async Task<IActionResult> EditTimeline(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timelineItem = await _context.TimelineItem.FindAsync(id);
            if (timelineItem == null)
            {
                return NotFound();
            }
            return PartialView("_TimelineItemForm", timelineItem);
        }

        // POST: Admin/EditTimeline/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimeline(int id, TimelineItem timelineItem)
        {
            if (id != timelineItem.EventID)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timelineItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TimelineItem.Any(e => e.EventID == timelineItem.EventID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Json(new { success = true });
            }
            return PartialView("_TimelineItemForm", timelineItem);
        }


        // GET: Admin/DeleteTimeline/1
        // คืนค่า Partial View ที่เป็นหน้ายืนยันการลบ
        public async Task<IActionResult> DeleteTimeline(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timelineItem = await _context.TimelineItem
                .FirstOrDefaultAsync(m => m.EventID == id);
            if (timelineItem == null)
            {
                return NotFound();
            }

            return PartialView("_DeleteTimelineItem", timelineItem);
        }

        // POST: Admin/DeleteTimeline/1
        [HttpPost, ActionName("DeleteTimeline")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimelineConfirmed(int id)
        {
            var timelineItem = await _context.TimelineItem.FindAsync(id);
            if (timelineItem != null)
            {
                _context.TimelineItem.Remove(timelineItem);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
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
            var contactsList = await _context.contacts
                                     .OrderBy(c => c.DisplayOrder) // *** สำคัญมาก: ต้องเรียงตาม DisplayOrder ***
                                     .ThenBy(c => c.id) // อาจจะเรียงตาม id ต่อก็ได้ถ้า DisplayOrder เท่ากัน
                                     .ToListAsync();
            return View(contactsList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    // *** เพิ่มส่วนนี้เข้าไป ***
                    // หา DisplayOrder ที่มากที่สุด แล้วบวก 1 เพื่อให้รายการใหม่อยู่ท้ายสุด
                    var maxOrder = await _context.contacts.AnyAsync() ? await _context.contacts.MaxAsync(c => c.DisplayOrder) : 0;
                    contactData.DisplayOrder = maxOrder + 1;
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

            // 1. ดึงข้อมูล record เก่าทั้งหมดก่อน เพื่อเอา URL รูปเก่า
            var record = await _context.contacts.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            if (record == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูล" });
            }

            // 2. เก็บ URL ของรูปเก่าไว้ก่อนที่จะทำการบันทึกรูปใหม่
            var oldImageUrl = record.ImageUrl;

            // 3. บันทึกรูปใหม่ที่ crop แล้ว และรับ URL ใหม่กลับมา
            var newImageUrl = await SaveCroppedImage(croppedImage); // ไม่ต้องส่ง id ไปแล้ว

            if (string.IsNullOrEmpty(newImageUrl))
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกรูปภาพ" });
            }

            // 4. อัปเดตฐานข้อมูลด้วย URL ใหม่
            await _context.contacts
                .Where(x => x.id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ImageUrl, newImageUrl));

            // 5. ลบไฟล์รูปเก่า (ถ้ามี และไม่ใช่ค่า default)
            if (!string.IsNullOrEmpty(oldImageUrl))
            {
                // สร้าง path จริงจาก URL ที่เก็บใน DB
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    catch (IOException ex)
                    {
                        // สามารถ Log error ไว้ดูได้ แต่ไม่จำเป็นต้องหยุดการทำงาน
                        Console.WriteLine($"Error deleting old file: {ex.Message}");
                    }
                }
            }

            return Json(new { success = true, newImageUrl });
        }

        private async Task<string> SaveCroppedImage(string base64Image)
        {
            // แยกข้อมูล base64 ออกจากส่วนหัว
            var base64Data = base64Image.Split(',')[1];
            var imageBytes = Convert.FromBase64String(base64Data);

            // สร้างชื่อไฟล์ใหม่ที่ไม่ซ้ำกันด้วย Guid
            var fileName = $"contact_{Guid.NewGuid()}.jpg";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/contacts");

            // ตรวจสอบว่ามีโฟลเดอร์อยู่หรือไม่ ถ้าไม่มีให้สร้าง
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            // คืนค่าเป็น URL ที่จะใช้ในแท็ก <img>
            return $"/images/contacts/{fileName}";
        }
        [HttpPost] // ใช้ [HttpPost] เพื่อความปลอดภัย ป้องกันการลบผ่าน URL โดยตรง
        [ValidateAntiForgeryToken] // ป้องกันการโจมตีแบบ CSRF
        public async Task<IActionResult> DeleteContact(int id)
        {
            // 1. ค้นหาข้อมูลผู้ติดต่อ รวมถึง URL รูปภาพ
            var contact = await _context.contacts.AsNoTracking().FirstOrDefaultAsync(c => c.id == id);

            if (contact == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลที่จะลบ" });
            }

            // 2. ลบไฟล์รูปภาพเก่าออกจากเซิร์ฟเวอร์ (ถ้ามี)
            if (!string.IsNullOrEmpty(contact.ImageUrl) && !contact.ImageUrl.EndsWith("default-user.png")) // ไม่ลบรูป default
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contact.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (IOException ex)
                    {
                        // สามารถ Log error ไว้ดูได้ แต่ไม่จำเป็นต้องหยุดการทำงาน
                        Console.WriteLine($"Error deleting file: {ex.Message}");
                    }
                }
            }

            // 3. ลบข้อมูลออกจากฐานข้อมูล
            // ใช้ ExecuteDeleteAsync เพื่อประสิทธิภาพที่ดีกว่า
            await _context.contacts.Where(c => c.id == id).ExecuteDeleteAsync();

            // 4. ส่งผลลัพธ์กลับไปในรูปแบบ JSON
            return Json(new { success = true, message = "ลบข้อมูลสำเร็จ" });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<int> orderedIds)
        {
            if (orderedIds == null || !orderedIds.Any())
            {
                return BadRequest("No order data provided.");
            }

            // ดึงข้อมูลผู้ติดต่อทั้งหมดที่มี ID ตรงกับที่ส่งมา
            try
            {
                // ดึงข้อมูลผู้ติดต่อทั้งหมดที่มี ID ตรงกับที่ส่งมา "ทั้งหมดในครั้งเดียว" เพื่อประสิทธิภาพ
                var contactsToUpdate = await _context.contacts
                                                     .Where(c => orderedIds.Contains(c.id))
                                                     .ToListAsync();

                // **นี่คือหัวใจสำคัญ:** วนลูปตามลำดับ ID ที่ได้รับมาจากหน้าบ้าน
                for (int i = 0; i < orderedIds.Count; i++)
                {
                    var contactId = orderedIds[i]; // ID ของรายการ
                    var newOrder = i + 1;         // ลำดับใหม่ที่ควรจะเป็น

                    // สร้างคำสั่ง UPDATE โดยตรงไปยังฐานข้อมูล
                    // โดยไม่ต้องดึงข้อมูลมาไว้ในหน่วยความจำก่อน
                    await _context.contacts
                        .Where(c => c.id == contactId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.DisplayOrder, newOrder));
                }

                // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูลในครั้งเดียว
                //await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "บันทึกลำดับสำเร็จ" });
            }
            catch (Exception ex)
            {
                // กรณีเกิดข้อผิดพลาด ให้ส่ง error กลับไปดู
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> frmlistregisterAll(string c_id, string a_id)
        {
            var datasetting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            if (datasetting == null) return View(new AdminRegistrationViewModel());

            int categoryId = 0;
            int affiliationId = 0;
            int.TryParse(c_id, out categoryId);
            int.TryParse(a_id, out affiliationId);

            // เพิ่ม Context สำหรับ Count แยก เพื่อความเร็ว
            using (var contextA = _contextFactory.CreateDbContext()) // Table Data
            using (var contextB = _contextFactory.CreateDbContext()) // Count Teams
            using (var contextC = _contextFactory.CreateDbContext()) // Category Stats
            using (var contextD = _contextFactory.CreateDbContext()) // Count Students
            using (var contextE = _contextFactory.CreateDbContext()) // Count Teachers
            {
                // ฟังก์ชันช่วยสร้าง Query Base (กรอง Setting, Category, Affiliation)
                IQueryable<Registerhead> BuildQuery(ApplicationDbContext ctx)
                {
                    var q = ctx.Registerhead.Where(x => x.SettingID == datasetting.id).AsNoTracking();
                    if (categoryId != 0) q = q.Where(rh => rh.Competitionlist.c_id == categoryId);
                    if (affiliationId != 0) q = q.Where(rh => rh.School.a_id == affiliationId);
                    return q;
                }

                // ---------------------------------------------------------
                // Task 1: ดึงข้อมูลตาราง (ใช้ AsSplitQuery เพื่อแก้ Timeout)
                // ---------------------------------------------------------
                var tableDataTask = BuildQuery(contextA)
                    .Include(rh => rh.School)
                    .Include(rh => rh.Competitionlist).ThenInclude(cl => cl.Category)
                    .Include(rh => rh.Registerdetail)
                    .AsSplitQuery() // ⚡️ (สำคัญมาก) แยก Query เพื่อลดภาระการ Join
                    .Select(rh => new RegisterViewModel2
                    {
                        Id = rh.id,
                        SchoolName = rh.School.Name,
                        CompetitionName = rh.Competitionlist.Name,
                        Award = rh.award,
                        Rank = rh.rank.ToString(),
                        Students = rh.Registerdetail.Where(d => d.Type == "student")
                            .Select(s => new PersonViewModel2 { No = s.no, Prefix = s.Prefix, FirstName = s.FirstName, LastName = s.LastName })
                            .OrderBy(s => s.No).ToList(),
                        Teachers = rh.Registerdetail.Where(d => d.Type == "teacher")
                            .Select(t => new PersonViewModel2 { No = t.no, Prefix = t.Prefix, FirstName = t.FirstName, LastName = t.LastName })
                            .OrderBy(t => t.No).ToList()
                    })
                    .OrderBy(x => x.SchoolName)
                    .ToListAsync();

                // ---------------------------------------------------------
                // Task 2: นับยอดรวม (แยกเป็น 3 Query ย่อย เร็วกว่า GroupBy ใหญ่)
                // ---------------------------------------------------------
                var countTeamsTask = BuildQuery(contextB).CountAsync();

                var countStudentsTask = BuildQuery(contextD)
                    .SelectMany(rh => rh.Registerdetail)
                    .CountAsync(d => d.Type == "student");

                var countTeachersTask = BuildQuery(contextE)
                    .SelectMany(rh => rh.Registerdetail)
                    .CountAsync(d => d.Type == "teacher");

                // ---------------------------------------------------------
                // Task 3: สรุปยอดแยกตามหมวดหมู่ (เหมือนเดิม)
                // ---------------------------------------------------------
                var categoryStatsTask = contextC.category
                    .Where(c => c.status == "1")
                    .Select(cat => new CategoryStatViewModel
                    {
                        CategoryName = cat.Name,
                        Competitions = cat.competitionlists
                            .Where(comp => comp.status == "1")
                            .Select(comp => new CompetitionStatViewModel
                            {
                                CompetitionName = comp.Name,
                                TeamCount = comp.registerheads.Count(rh => rh.SettingID == datasetting.id && rh.status == "1"),
                                StudentCount = comp.registerheads.Where(rh => rh.SettingID == datasetting.id && rh.status == "1")
                                                .SelectMany(rh => rh.Registerdetail).Count(rd => rd.Type == "student"),
                                TeacherCount = comp.registerheads.Where(rh => rh.SettingID == datasetting.id && rh.status == "1")
                                                .SelectMany(rh => rh.Registerdetail).Count(rd => rd.Type == "teacher")
                            })
                            .OrderByDescending(c => c.TeamCount).ToList()
                    })
                    .Select(catViewModel => new CategoryStatViewModel
                    {
                        CategoryName = catViewModel.CategoryName,
                        Competitions = catViewModel.Competitions,
                        TeamCount = catViewModel.Competitions.Sum(c => c.TeamCount),
                        CompetitionCount = catViewModel.Competitions.Count
                    })
                    .OrderBy(x => x.CategoryName)
                    .ToListAsync();

                // 4. รอผลลัพธ์ทั้งหมด (Run 5 Tasks Parallel)
                await Task.WhenAll(tableDataTask, countTeamsTask, countStudentsTask, countTeachersTask, categoryStatsTask);

                // 5. ประกอบร่าง ViewModel
                var viewModel = new AdminRegistrationViewModel
                {
                    Registrations = tableDataTask.Result,
                    CategoryStats = categoryStatsTask.Result,
                    TotalTeams = countTeamsTask.Result,       // ผลลัพธ์จาก Task ย่อย
                    TotalStudents = countStudentsTask.Result, // ผลลัพธ์จาก Task ย่อย
                    TotalTeachers = countTeachersTask.Result, // ผลลัพธ์จาก Task ย่อย
                    TotalCompetitions = categoryStatsTask.Result.Sum(c => c.CompetitionCount)
                };

                // 6. เตรียม Dropdowns
                ViewBag.CategoryList = new SelectList(await _context.category.Where(x => x.status == "1").ToListAsync(), "Id", "Name", categoryId);
                ViewBag.AffiliationList = new SelectList(await _context.Affiliation.ToListAsync(), "Id", "Name", affiliationId);

                return View(viewModel);
            }
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
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCriterionModal(int id)
        {
            var criterion = await _context.criterion.FindAsync(id);
            if (criterion == null)
            {
                return NotFound();
            }

            // เราจะสร้าง Partial View (_EditCriterionPartial.cshtml)
            // เพื่อเป็น "เนื้อใน" ของ Modal
            return PartialView("_EditCriterionPartial", criterion);
        }

        // --- Action 2: (POST) สำหรับรับข้อมูลที่แก้ไขจาก Modal ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCriterionModal(criterion model)
        {
            // เราไม่บังคับอัปโหลดไฟล์เสมอไป ให้ลบ Error นี้ออกถ้ามี
            ModelState.Remove("PdfFile");

            if (ModelState.IsValid)
            {
                // 1. โหลดข้อมูลเดิมจาก DB
                var criterionInDb = await _context.criterion.FindAsync(model.id);
                if (criterionInDb == null) return NotFound();

                string oldFileName = criterionInDb.file; // 2. เก็บชื่อไฟล์เก่าไว้

                // 3. อัปเดตชื่อ
                criterionInDb.name = model.name;

                // 4. ตรวจสอบว่ามีการอัปโหลดไฟล์ใหม่หรือไม่
                if (model.PdfFile != null && model.PdfFile.Length > 0)
                {
                    // 4a. กำหนด Path (เช่น: wwwroot/pdf)
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "pdf");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // 4b. สร้างชื่อไฟล์ใหม่ (กันซ้ำ)
                    string newFileName = $"{Guid.NewGuid()}_{model.PdfFile.FileName}";
                    string newFilePath = Path.Combine(uploadPath, newFileName);

                    // 4c. บันทึกไฟล์ใหม่
                    using (var stream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await model.PdfFile.CopyToAsync(stream);
                    }

                    // 4d. อัปเดตชื่อไฟล์ใน DB
                    criterionInDb.file = newFileName;

                    // 4e. ⚡️ ลบไฟล์เก่า (ถ้ามี)
                    if (!string.IsNullOrEmpty(oldFileName))
                    {
                        string oldFilePath = Path.Combine(uploadPath, oldFileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                // 5. บันทึกการเปลี่ยนแปลง
                _context.Update(criterionInDb);
                await _context.SaveChangesAsync();

                // 6. ส่ง JSON บอกว่าสำเร็จ
                return Json(new { success = true });
            }

            // 7. ถ้า ModelState ไม่ผ่าน, ส่งฟอร์มกลับไป (พร้อม Error)
            return PartialView("_EditCriterionPartial", model);
        }
        // --- Action (GET) สำหรับเปิด Modal "สร้าง" ---
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateCriterionModal(int settingId, int g_id)
        {
            // 1. สร้าง Model เปล่า พร้อม "ยัด" ID ที่ถูกต้องเข้าไป
            var model = new criterion
            {
                SettingID = settingId,
                g_id = g_id,
                status = "1" // (ค่าเริ่มต้น)
            };

            // 2. ใช้ Partial View เดียวกับตอน Edit
            return PartialView("_EditCriterionPartial", model);
        }

        // --- Action (POST) สำหรับ "สร้าง" ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCriterionModal(criterion model)
        {
            // เราไม่บังคับอัปโหลดไฟล์ตอนสร้าง
            ModelState.Remove("PdfFile");
            ModelState.Remove("file"); // (อาจจะไม่จำเป็น แต่กันเหนียว)

            if (ModelState.IsValid)
            {
                // 1. (ถ้ามีไฟล์) บันทึกไฟล์ PDF (เหมือน Logic Edit)
                if (model.PdfFile != null && model.PdfFile.Length > 0)
                {
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "pdf");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    string newFileName = $"{Guid.NewGuid()}_{model.PdfFile.FileName}";
                    string newFilePath = Path.Combine(uploadPath, newFileName);

                    using (var stream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await model.PdfFile.CopyToAsync(stream);
                    }
                    model.file = newFileName; // อัปเดตชื่อไฟล์
                }

                // 2. เพิ่มข้อมูล (SettingID, g_id ถูกส่งมาจากฟอร์ม)
                _context.criterion.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            // 3. ถ้าไม่ผ่าน, ส่งฟอร์มกลับไป
            return PartialView("_EditCriterionPartial", model);
        }

        // --- 1. (GET) หน้าหลักสำหรับแสดงรายการ ---
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CategoryList()
        {
            var categories = await _context.category.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        // --- 2. (GET) สำหรับปุ่ม "สร้าง" ---
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateCategoryModal()
        {
            var model = new category { status = "1" }; // ตั้งค่าเริ่มต้น status=1
            return PartialView("_CategoryModalPartial", model);
        }

        // --- 3. (POST) สำหรับ "บันทึก" (การสร้างใหม่) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategoryModal([Bind("Name, fullname, status")] category model)
        {
            if (ModelState.IsValid)
            {
                _context.category.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return PartialView("_CategoryModalPartial", model);
        }

        // --- 4. (GET) สำหรับปุ่ม "แก้ไข" ---
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCategoryModal(int id)
        {
            var model = await _context.category.FindAsync(id);
            if (model == null) return NotFound();
            return PartialView("_CategoryModalPartial", model);
        }

        // --- 5. (POST) สำหรับ "บันทึก" (การแก้ไข) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCategoryModal(int id, [Bind("Id, Name, fullname, status")] category model)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.category.Any(e => e.Id == model.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return PartialView("_CategoryModalPartial", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // 1. ค้นหา Category พร้อม "Include" ข้อมูลที่ผูกอยู่
            var category = await _context.category
                .Include(c => c.competitionlists) // 👈 (สำคัญ) โหลดรายการแข่งขันที่ผูกอยู่
                .Include(c => c.groupreferee)   // 👈 (สำคัญ) โหลดกลุ่มกรรมการที่ผูกอยู่
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // 2. ⚡️ (สำคัญมาก) ตรวจสอบว่าถูกใช้งานหรือไม่
            if (category.competitionlists.Any() || category.groupreferee.Any())
            {
                // 3. ถ้าถูกใช้งาน: ห้ามลบ! ให้ส่ง Error กลับไป
                TempData["MessageTitle"] = "ลบไม่สำเร็จ";
                TempData["MessageText"] = "ไม่สามารถลบหมวดหมู่นี้ได้ เนื่องจากมี 'รายการแข่งขัน' หรือ 'กลุ่มกรรมการ' ผูกอยู่";
                TempData["MessageIcon"] = "error"; // ไอคอนสีแดง

                return RedirectToAction(nameof(CategoryList));
            }

            // 4. ถ้าไม่ถูกใช้งาน: ลบได้
            _context.category.Remove(category);
            await _context.SaveChangesAsync();

            TempData["MessageTitle"] = "ลบสำเร็จ";
            TempData["MessageText"] = "ข้อมูลหมวดหมู่ถูกลบเรียบร้อยแล้ว";
            TempData["MessageIcon"] = "success"; // ไอคอนสีเขียว

            return RedirectToAction(nameof(CategoryList));
        }
        // --- 6. (POST) ⚡️ (สำคัญ) สำหรับปุ่ม "เปิด/ปิด" ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.category.FindAsync(id);
            if (category == null) return NotFound();

            // สลับค่า status
            category.status = (category.status == "1") ? "0" : "1";

            _context.Update(category);
            await _context.SaveChangesAsync();

            // กลับไปหน้า List เดิม
            return RedirectToAction(nameof(CategoryList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCriterion(int id)
        {
            var criterion = await _context.criterion.FindAsync(id);
            if (criterion == null)
            {
                return NotFound();
            }

            // --- (สำคัญ) ตรวจสอบ Dependencies ---
            // (สมมติว่าตาราง Competitionlist มี Foreign Key ชื่อ CriterionId)
            // ⚠️ ถ้าคุณมีตารางอื่นที่ผูกกับ criterion ให้ Include มาเช็กที่นี่
            // var isUsed = await _context.Competitionlist.AnyAsync(c => c.CriterionId == id);
            // if (isUsed)
            // {
            //     TempData["MessageTitle"] = "ลบไม่สำเร็จ";
            //     TempData["MessageText"] = "ไม่สามารถลบเกณฑ์นี้ได้ เพราะมี 'รายการแข่งขัน' ผูกอยู่";
            //     TempData["MessageIcon"] = "error";
            //     return RedirectToAction(nameof(CriterionList));
            // }

            // ------------------------------------

            // 1. เก็บชื่อไฟล์เก่าไว้
            string oldFileName = criterion.file;

            // 2. ลบข้อมูลออกจากฐานข้อมูล
            _context.criterion.Remove(criterion);
            await _context.SaveChangesAsync();

            // 3. ⚡️ ลบไฟล์ PDF เก่าออกจาก Server
            DeleteCriterionFile(oldFileName); // (เรียก Helper Method ที่คุณมีอยู่แล้ว)

            TempData["MessageTitle"] = "ลบสำเร็จ";
            TempData["MessageText"] = "ข้อมูลเกณฑ์ถูกลบเรียบร้อยแล้ว";
            TempData["MessageIcon"] = "success";

            return RedirectToAction("frmcriterion", "Home");
        }
        private void DeleteCriterionFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            // (สำคัญ) "pdf" คือโฟลเดอร์ที่คุณเก็บไฟล์เกณฑ์
            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "pdf", fileName);
            try
            {
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }
            catch (Exception ex)
            {
                // (Optional) Log error, แต่ไม่หยุดการทำงาน
                _logger.LogError(ex, $"Error deleting file {fileName}");
            }
        }
        // (อย่าลืม Inject _context, _userManager)

        [Authorize(Roles = "Admin,Manager")] // 👈 (อนุญาตให้ Admin และ Manager เข้า)
        public async Task<IActionResult> Dashboard() // (หรือใช้ชื่อ Index() ก็ได้)
        {
            // 1. เตรียม ViewModel
            var model = new AdminDashboardViewModel();

            var settingId = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();

            // 2. ดึงข้อมูลสรุปยอดรวม (KPIs)
            model.TotalUsers = await _userManager.Users.CountAsync();
            model.TotalSchools = await _context.school.CountAsync(s => s.status == "1");
            model.TotalCompetitions = await _context.Competitionlist.CountAsync(c => c.status == "1");

            // (สมมติว่า Registerhead คือตารางเก็บทีมที่สมัคร)
            model.TotalRegistrations = await _context.Registerhead.Where(x => x.SettingID == settingId.id).CountAsync();

            model.TotalStudents = await _context.Registerdetail.Where(x => x.Registerhead.SettingID == settingId.id && x.Type == "student").CountAsync();

            // (สมมติว่า 'Registerteacher' คือตารางครูผู้ฝึกซ้อม)
            model.TotalTeachers = await _context.Registerdetail.Where(x => x.Registerhead.SettingID == settingId.id && x.Type == "teacher").CountAsync();

            // (สมมติว่า 'Referee' คือชื่อ Role ของกรรมการ)
            var judges = await _userManager.GetUsersInRoleAsync("Referee");
            model.TotalJudges = judges.Count;

            model.CategoryStats = await _context.Registerhead
                    .Where(x => x.SettingID == settingId.id)
                    .Include(rh => rh.Competitionlist) // 1. โหลดข้อมูลการแข่งขันที่ผูกอยู่
                        .ThenInclude(cl => cl.Category) // 2. จากการแข่งขัน โหลดหมวดหมู่ที่ผูกอยู่
                    .GroupBy(rh => rh.Competitionlist.Category.Name) // 3. "จัดกลุ่ม" ด้วย "ชื่อหมวดหมู่"
                    .Select(g => new CategoryRegistrationCount // 4. สร้าง object ใหม่
                    {
                        CategoryName = g.Key, // g.Key คือ "ชื่อหมวดหมู่"
                        TeamCount = g.Count() // g.Count() คือ "จำนวนทีม" ในกลุ่มนั้น
                    })
                    .OrderByDescending(stats => stats.TeamCount) // 5. เรียงจากฮิตที่สุด
                    .ToListAsync();
            // 3. ดึงข้อมูลสถิติ (จากตาราง VisitorCounts ที่เราเคยทำ)
            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;

            model.DailyVisits = (await _context.VisitorCounts
                .FirstOrDefaultAsync(vc => vc.VisitDate == today))?
                .VisitCount ?? 0;

            model.MonthlyVisits = (await _context.VisitorCounts
                .FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == month && vc.Week == 0))?
                .VisitCount ?? 0;

            model.YearlyVisits = (await _context.VisitorCounts
                .FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == 0 && vc.Week == 0))?
                .VisitCount ?? 0;

            // 4. ส่ง Model ไปที่ View
            return View(model);
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> SchoolRegistrationReport(string g_id, string a_id)
        {
            var settingId = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            int g_val = 0;
            int a_val = 0;
            int.TryParse(g_id, out g_val);
            int.TryParse(a_id, out a_val);

            // 1. เริ่ม Query (กรองเฉพาะที่ Active)
            var query = _context.school
                .AsNoTracking()
                .Where(s => s.status == "1");

            // 2. ใช้ฟิลเตอร์ (ถ้ามี)
            if (g_val != 0)
            {
                query = query.Where(s => s.g_id == g_val);
            }
            if (a_val != 0)
            {
                query = query.Where(s => s.a_id == a_val);
            }

            // 3. ⚡️ (สำคัญ) ใช้ .Select() เพื่อ Project ข้อมูล
            //    (Query นี้จะทำงานได้ต่อเมื่อ Model 'Registerhead' 
            //     มี ICollection 'Registerstudents' และ 'Registerteachers')
            var reportData = await query
                .Include(s => s.Affiliation) // โหลดสังกัด
                .Include(s => s.grouplist)   // โหลดกลุ่ม
                .Include(s => s.registerheads.Where(x => x.SettingID == settingId.id)) // โหลดทีม
                    .ThenInclude(rh => rh.Registerdetail)
                .Select(s => new SchoolReportViewModel
                {
                    SchoolId = s.Id,
                    SchoolName = s.Name,
                    AffiliationName = s.Affiliation.Name ?? "-",
                    GroupName = s.grouplist.Name ?? "-",

                    // 4. ให้ Database "นับ" ข้อมูล
                    TeamCount = s.registerheads.Where(x => x.SettingID == settingId.id).Count(),
                    StudentCount = s.registerheads.SelectMany(rh => rh.Registerdetail).Count(x => x.Type == "student" && x.Registerhead.SettingID == settingId.id),
                    TeacherCount = s.registerheads.SelectMany(rh => rh.Registerdetail).Count(x => x.Type == "teacher" && x.Registerhead.SettingID == settingId.id)
                })
                .OrderBy(s => s.SchoolName)
                .ToListAsync();

            // 5. ส่งข้อมูล Dropdown กลับไปที่ View
            ViewBag.levelData = new SelectList(await _context.grouplist.ToListAsync(), "Id", "Name", g_val);
            ViewBag.affiliationData = new SelectList(await _context.Affiliation.ToListAsync(), "Id", "Name", a_val);

            return View(reportData);
        }
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> frmCompetitionSchedule()
        {
            // 1. ดึงงานปัจจุบัน
            var activeSetting = await _context.setupsystem
                .FirstOrDefaultAsync(s => s.status == "1");

            if (activeSetting == null)
            {
                return View(new List<AdminScheduleViewModel>());
            }

            // 2. ดึงข้อมูล racedetails เชื่อมกับตารางอื่นๆ
            var data = await _context.racedetails
                .AsNoTracking()
                .Where(rd => rd.SettingID == activeSetting.id && rd.status == "1") // เฉพาะงานปัจจุบันและสถานะ Active
                .Include(rd => rd.Competitionlist)
                    .ThenInclude(c => c.Category)
                .Include(rd => rd.Racelocation)
                .Select(rd => new AdminScheduleViewModel
                {
                    Id = rd.id,
                    CompetitionId = rd.c_id,
                    CompetitionName = rd.Competitionlist.Name,
                    CategoryName = rd.Competitionlist.Category.Name,
                    DateRange = rd.daterace, // (เดี๋ยวไปแปลงเป็นไทยใน View)
                    Time = rd.time,
                    LocationName = rd.Racelocation.name,
                    Building = rd.building,
                    Room = rd.room,
                    Status = rd.status
                })
                .OrderBy(x => x.CategoryName) // เรียงตามหมวดหมู่
                .ThenBy(x => x.CompetitionName) // แล้วเรียงตามชื่อรายการ
                .ToListAsync();

            ViewBag.ActiveSettingName = activeSetting.name;

            return View(data);
        }
    }
}