using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Silapa.Models;
using Syncfusion.EJ2.Linq;

namespace Silapa.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly LineAlert _LineAlert = new LineAlert();
        private readonly IWebHostEnvironment _webHostEnvironment;
        public MemberController(ILogger<AdminController> logger, ApplicationDbContext connectDbContext, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = connectDbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> frmRegister(string c_id)
        {
            var user = await _userManager.GetUserAsync(User);
            var datatsetting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            if (datatsetting != null)
            {
                //ลงทะเบียน
                string[] startdate;
                string[] enddate;
                DateTime todate = DateTime.Now; // วันที่ปัจจุบัน
                startdate = datatsetting.startregisterdate.ToString().Split('/');
                enddate = datatsetting.endgisterdate.ToString().Split('/');
                DateTime startDateNow = new DateTime(Convert.ToInt16(startdate[2]), Convert.ToInt16(startdate[1]),
                Convert.ToInt16(startdate[0]));
                DateTime endDateNow = new DateTime(Convert.ToInt16(enddate[2]), Convert.ToInt16(enddate[1]),
                Convert.ToInt16(enddate[0]));
                /////
                ///แก้ไข
                ///
                string[] starteditdate;
                string[] endeditdate;
                starteditdate = datatsetting.startedit.ToString().Split('/');
                endeditdate = datatsetting.endedit.ToString().Split('/');
                DateTime starteditDateNow = new DateTime(Convert.ToInt16(starteditdate[2]), Convert.ToInt16(starteditdate[1]),
                Convert.ToInt16(starteditdate[0]));
                DateTime endeditDateNow = new DateTime(Convert.ToInt16(endeditdate[2]), Convert.ToInt16(endeditdate[1]),
                Convert.ToInt16(endeditdate[0]));


                // เรียกฟังก์ชันและรับค่า
                bool isInRange = DateHelper.IsCurrentDateInRange(startDateNow, endDateNow);

                ViewBag.statusdateregister = isInRange ? "1" : "0";
                // เรียกฟังก์ชันและรับค่า
                bool isInRangeedit = DateHelper.IsCurrentDateInRange(starteditDateNow, endeditDateNow);

                ViewBag.statusdateedit = isInRangeedit ? "1" : "0";



            }
            int a = Convert.ToInt32(c_id);

            var data = _context.Competitionlist.Where(x => x.status == "1").ToList();
            if (c_id != null)
            {
                data = data.Where(x => x.c_id == a).ToList();
            }
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = a;


            ViewBag.filepdf = await _context.uploadfilepdf.Where(x => x.s_id == user.s_id).FirstOrDefaultAsync();

            var activeSettingIds = await _context.setupsystem
               .Where(s => s.status == "1")
               .Select(s => s.id)
               .ToListAsync();

            var datah = await _context.Registerhead.Where(x => x.s_id == user.s_id && x.status == "1" && activeSettingIds.Contains(x.SettingID)).Include(x => x.Registerdetail).ToListAsync();
            ViewBag.data = datah;
            return View(data.OrderBy(x => x.Id));
        }
        public async Task<IActionResult> frmuploadpdf(int c_id, int s_id, string msg)
        {
            ViewBag.s_id = s_id;
            ViewBag.c_id = c_id;
            var datapdf = await _context.uploadfilepdf.Where(x => x.c_id == c_id && x.s_id == s_id).FirstOrDefaultAsync();
            if (datapdf != null)
            {
                return View(datapdf);
            }
            ViewBag.msg = "กรุณาอัพโหลดไฟล์ PDF ประกอบการสมัครก่อนโดยรวมไฟล์เป็นไฟล์เดียว";

            return View(new uploadfilepdf());
        }
        [HttpPost]
        public async Task<IActionResult> frmuploadpdf(IFormFile pdfUpload, [FromForm] uploadfilepdf model)
        {
            // กำหนด HTTP Header ป้องกันการแคช
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";
            if (pdfUpload != null && pdfUpload.Length > 0)
            {
                // ตรวจสอบว่าไฟล์เป็น PDF หรือไม่
                if (Path.GetExtension(pdfUpload.FileName).ToLower() != ".pdf")
                {
                    ModelState.AddModelError("pdfUpload", "กรุณาเลือกไฟล์ PDF เท่านั้น");
                    return View(model);
                }
                // สร้างชื่อไฟล์ใหม่โดยใช้ GUID และคงนามสกุล .pdf
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(pdfUpload.FileName).ToLower();
                // กำหนดที่เก็บไฟล์
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads/pdf", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfUpload.CopyToAsync(stream);
                }
                model.filename = fileName;

                model.lastupdate = DateTime.Now;
                // ถ้าต้องการบันทึกข้อมูล model อื่นๆ
                if (model.id == 0)
                {
                    model.status = "1";
                    model.msg = "รอการตรวจสอบเอกสาร";
                    _context.uploadfilepdf.Add(model);
                }
                else
                {
                    //model.status = "1";
                    //_context.uploadfilepdf.Update(model);
                    await _context.uploadfilepdf.Where(x => x.id == model.id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(x => x.filename, fileName)
                        .SetProperty(x => x.status, "1")
                        .SetProperty(x => x.msg, "ส่งไฟล์ใหม่อีกรอบ")
                        .SetProperty(x => x.lastupdate, DateTime.Now)
                    );
                }

                await _context.SaveChangesAsync();
                if (model.id != 0)
                {
                    return RedirectToAction("frmRegister");
                }
            }
            var data = _context.Competitionlist.Where(x => x.Id == model.c_id).FirstOrDefault();
            return RedirectToAction("frmRegister_add", new { c_id = data.Id, c_name = data.Name, type = data.type, t = data.teacher, s = data.student });
        }
        public async Task<IActionResult> frmRegister_add(int id, int c_id, int s_id, string c_name, string type, int t, int s)
        {

            var user = await _userManager.GetUserAsync(User);
            if (s_id == 0)
            {
                s_id = Convert.ToInt16(user.s_id);
                ViewBag.s_id = s_id;
            }
            if (c_id == 43)
            {
                var datapdf = await _context.uploadfilepdf.Where(x => x.c_id == c_id && x.s_id == s_id).FirstOrDefaultAsync();
                if (datapdf == null)
                {

                    return RedirectToAction("frmuploadpdf", new { c_id = c_id, s_id = s_id });
                }
            }
            if (id == 0)
            {
                ViewBag.c_id = c_id;
                ViewBag.s_id = s_id;
                ViewBag.c_name = c_name;
                ViewBag.type = type;
                ViewBag.t = t;
                ViewBag.s = s;
                return View(new Registerhead());
            }
            else
            {
                ViewBag.s_id = s_id;
                ViewBag.c_id = c_id;
                ViewBag.c_name = c_name;
                ViewBag.type = type;
                ViewBag.t = t;
                ViewBag.s = s;
                var data = await _context.Registerdetail.Where(x => x.h_id == id).ToListAsync();
                ViewBag.registerdetails = data;
                return View(_context.Registerhead.Find(id));
            }

        }

        public async Task<IActionResult> frmRegister_del(int id, int c_id)
        {
            var user = await _userManager.GetUserAsync(User);
            await _context.Registerhead.Where(x => x.id == id).ExecuteUpdateAsync(x => x.SetProperty(
                x => x.status, "0")
                .SetProperty(x => x.lastupdate, DateTime.Now)
                .SetProperty(x => x.u_id, user.Id)
            );
            if (User.IsInRole("Member"))
            {
                return RedirectToAction("frmRegister");
            }
            else if (User.IsInRole("Manager"))
            {
                return RedirectToAction("frmmanagerscore", "Manager", new { c_id = c_id });
            }
            return View();
        }
        /*
        [HttpPost]
        public async Task<IActionResult> SaveStudents([FromBody] StudentTeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Students[0].c_id == 43)
                {
                    if (User.IsInRole("Member"))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "เนื่องจากรายการนี้ต้องมีการตรวจสอบเอกสารประกอบการแข่งขันไม่สามารถแก้ไขข้อมูลได้ ถ้าต้องการแก้ไขกรุณาติดต่อผู้ประสานงานกิจกรรมนี้"
                        });
                    }

                }
                var user = await _userManager.GetUserAsync(User);


                var msg = "";

                var schoolname = await _context.school.Where(x => x.Id == (int)model.Students[0].s_id).FirstOrDefaultAsync();
                var listname = await _context.Competitionlist.Where(x => x.Id == model.Students[0].c_id).FirstOrDefaultAsync();

                // 1. ดึง ID ของงานที่ Active ทั้งหมด
                var activeSettingIds = await _context.setupsystem
                    .Where(s => s.status == "1")
                    .Select(s => s.id)
                    .FirstOrDefaultAsync();
                var competitionId = model.Students[0].c_id;
                var existingRegisterHead = await _context.Registerhead
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.s_id == schoolname.Id &&
                                                      x.c_id == competitionId && // ใช้ตัวแปรที่ปลอดภัยกว่า
                                                      x.SettingID == activeSettingIds);
                await _context.Database.BeginTransactionAsync();
                try
                {
                    int headId = 0;

                    if (existingRegisterHead == null) // ถ้ายังไม่มี ให้เพิ่มใหม่
                    {
                        msg += "การลงทะเบียน " + schoolname.Name ?? "";
                        msg += " รายการ " + listname.Name ?? "";
                        var registerHead = new Registerhead
                        {
                            s_id = (int)user.s_id,
                            u_id = user.Id,
                            c_id = competitionId,
                            score = 0,
                            status = "1",
                            lastupdate = DateTime.Now,
                            SettingID = activeSettingIds,
                            award = "รอผลการตัดสิน",
                            rank = 0
                        };
                        _context.Registerhead.Add(registerHead);
                        await _context.SaveChangesAsync(); // บันทึกเพื่อให้ได้ค่า id ของ Registerhead
                                                           // ใช้ id ของ Registerhead ที่บันทึกแล้วสำหรับ Registerdetail

                        headId = registerHead.id;
                    }
                    else
                    {
                        msg += "การแก้ไขลงทะเบียน " + schoolname.Name ?? "";
                        msg += " รายการ " + listname.Name ?? "";
                        headId = existingRegisterHead.id;
                        if (existingRegisterHead.status == "0")
                        {
                            await _context.Registerhead.Where(x => x.id == headId)
                            .ExecuteUpdateAsync(x => x.SetProperty(x => x.status, "1"));
                        }
                    }
                    await _context.Registerdetail.Where(x => x.h_id == headId).ExecuteDeleteAsync();
                    // บันทึกข้อมูลนักเรียน
                    int nos = 1;
                    int not = 1;
                    msg += " นักเรียน";
                    foreach (var student in model.Students)
                    {
                        var newStudent = new Registerdetail
                        {
                            h_id = headId,
                            no = nos,
                            Prefix = student.Prefix,
                            FirstName = student.FirstName,
                            LastName = student.LastName,
                            ImageUrl = student.ImageUrl,
                            Type = "student",
                            u_id = user.Id,
                            c_id = student.c_id,
                            lastupdate = DateTime.Now
                        };
                        msg += " " + nos + "." + newStudent.Prefix + "" + newStudent.FirstName + " " + newStudent.LastName;
                        _context.Registerdetail.Add(newStudent);
                        nos += 1;
                    }
                    // บันทึกข้อมูลครู
                    msg += " ครู";
                    foreach (var teacher in model.Teachers)
                    {
                        var newTeacher = new Registerdetail
                        {
                            h_id = headId,
                            no = not,
                            Prefix = teacher.Prefix,
                            FirstName = teacher.FirstName,
                            LastName = teacher.LastName,
                            ImageUrl = teacher.ImageUrl,
                            Type = "teacher",
                            u_id = user.Id,
                            c_id = teacher.c_id,
                            lastupdate = DateTime.Now
                        };
                        msg += " " + not + "." + newTeacher.Prefix + "" + newTeacher.FirstName + " " + newTeacher.LastName;
                        _context.Registerdetail.Add(newTeacher);
                        not += 1;
                    }
                    msg += " เวลา " + DateTime.Now.ToString() + " บันทึกโดย " + user.titlename + "" + user.FirstName + " " + user.LastName;
                    await _context.SaveChangesAsync();
                    await _context.Database.CommitTransactionAsync();
                    // await _LineAlert.lineNotify(msg, _context.setupsystem.Where(x => x.id == 1).FirstOrDefault().token);

                    if (User.IsInRole("Member"))
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Results announced successfully!",
                            redirectUrl = Url.Action("frmRegister", "Member", new { c_id = model.Students[0].c_id }) // Generate the redirect URL
                        });
                        //return RedirectToAction("frmRegister");
                    }
                    else if (User.IsInRole("Manager"))
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Results announced successfully!",
                            redirectUrl = Url.Action("frmmanagerscore", "Manager", new { c_id = model.Students[0].c_id }) // Generate the redirect URL
                        });
                    }

                }
                catch (Exception ex)
                {
                    // Rollback transaction เมื่อเกิดข้อผิดพลาด
                    await _context.Database.RollbackTransactionAsync();
                    return BadRequest("Error saving data: " + ex.Message);
                }
            }
            return View(model);
        }*/
        [HttpPost]
        [ValidateAntiForgeryToken] // ⬅️ (แก้บั๊ก 4)
        public async Task<IActionResult> SaveStudents([FromBody] StudentTeacherViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var school = await _context.school.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.s_id);
            string schoolName = school?.Name ?? "Unknown";
            // --- (แก้บั๊ก 2) ตรวจสอบ Model ว่างเปล่า ---
            // แก้ไขเงื่อนไข Validation
            if (User.IsInRole("Member"))
            {
                // ถ้าเป็น Member ต้องมีข้อมูล
                if ((model.Students == null || !model.Students.Any()) &&
                    (model.Teachers == null || !model.Teachers.Any()))
                {
                    return Json(new { success = false, message = "กรุณาเพิ่มนักเรียนหรือครูอย่างน้อย 1 คน" });
                }
            }
            else
            {
                // ถ้าเป็น Manager/Admin ยอมให้ผ่านได้ (แม้ข้อมูลจะว่าง)
                // หรือ Initialize ให้เป็น List ว่าง เพื่อป้องกัน NullReferenceException ต่อไป
                if (model.Students == null) model.Students = new List<PersonViewModel>();
                if (model.Teachers == null) model.Teachers = new List<PersonViewModel>();
            }

            // --- (แก้บั๊ก 1) ตรวจสอบ ModelState ---
            if (!ModelState.IsValid)
            {
                // (ส่ง Error แรกที่เจอ)
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault();
                await LogAudit(user.Id, schoolName, "SaveStudents", false, $"ModelState Invalid: {error}");
                return Json(new { success = false, message = error?.ErrorMessage ?? "ข้อมูลไม่ถูกต้อง" });
            }

            // --- (แก้บั๊ก 2) ดึง ID อย่างปลอดภัย ---
            // (ดึง c_id และ s_id จาก "คนแรก" ที่หาเจอ ไม่ว่าจะเป็น Student หรือ Teacher)
            var firstPerson = (model.Students != null && model.Students.Any())
                              ? model.Students.First()
                              : model.Teachers.First();
            var competitionId = firstPerson.c_id;
            var schoolId = firstPerson.s_id;

            // --- (โค้ด Logic เดิมของคุณ) ---

            // (ตรวจสอบสิทธิ์การแก้ไข c_id 43)
            if (competitionId == 43 && User.IsInRole("Member"))
            {
                string errorMsg = "เนื่องจากรายการนี้ต้องมีการตรวจสอบเอกสารประกอบการแข่งขัน ไม่สามารถแก้ไขข้อมูลได้";
                await LogAudit(user.Id, schoolName, "SaveStudents", false, $"Blocked: {errorMsg} (c_id: 43)");
                return Json(new { success = false, message = errorMsg });
            }

            // var user = await _userManager.GetUserAsync(User);
            var msg = ""; // (สำหรับ Log)

            // (ดึงข้อมูลสำหรับ Log)
            //     var schoolname = await _context.school.AsNoTracking().FirstOrDefaultAsync(x => x.Id == schoolId);
            var listname = await _context.Competitionlist.AsNoTracking().FirstOrDefaultAsync(x => x.Id == competitionId);

            var activeSettingIds = await _context.setupsystem
                .Where(s => s.status == "1")
                .Select(s => s.id)
                .FirstOrDefaultAsync();

            var existingRegisterHead = await _context.Registerhead
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.s_id == schoolId &&
                                                  x.c_id == competitionId &&
                                                  x.SettingID == activeSettingIds);

            // (แก้บั๊ก 3) ใช้ using (ปลอดภัยกว่า)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    int headId = 0;

                    if (existingRegisterHead == null) // (สร้างใหม่)
                    {
                        msg += "การลงทะเบียน " + school?.Name ?? "";
                        msg += " รายการ " + listname?.Name ?? "";
                        var registerHead = new Registerhead
                        {
                            s_id = (int)user.s_id,
                            u_id = user.Id,
                            c_id = competitionId,
                            score = 0,
                            status = "1",
                            lastupdate = DateTime.Now,
                            SettingID = activeSettingIds,
                            award = "รอผลการตัดสิน",
                            rank = 0
                        };
                        _context.Registerhead.Add(registerHead);
                        await _context.SaveChangesAsync();
                        headId = registerHead.id;
                    }
                    else // (แก้ไข)
                    {
                        msg += "การแก้ไขลงทะเบียน " + school?.Name ?? "";
                        msg += " รายการ " + listname?.Name ?? "";
                        headId = existingRegisterHead.id;
                        if (existingRegisterHead.status == "0")
                        {
                            // (ใช้ Update/SetProperty ดีกว่า ExecuteUpdateAsync ใน Transaction)
                            existingRegisterHead.status = "1";
                            _context.Registerhead.Update(existingRegisterHead);
                        }
                    }

                    // (ลบของเก่า)
                    await _context.Registerdetail.Where(x => x.h_id == headId).ExecuteDeleteAsync();

                    // (เพิ่มนักเรียน)
                    int nos = 1;
                    msg += " นักเรียน";
                    if (model.Students != null)
                    {
                        foreach (var student in model.Students)
                        {
                            var newStudent = new Registerdetail
                            {
                                h_id = headId,
                                no = nos,
                                Prefix = student.Prefix,
                                FirstName = student.FirstName,
                                LastName = student.LastName,
                                ImageUrl = student.ImageUrl,
                                Type = "student",
                                u_id = user.Id,
                                c_id = student.c_id,
                                lastupdate = DateTime.Now
                            }; // (โค้ดเดิมของคุณ)
                            _context.Registerdetail.Add(newStudent);
                            nos += 1;
                        }
                    }

                    // (เพิ่มครู)
                    int not = 1;
                    msg += " ครู";
                    if (model.Teachers != null)
                    {
                        foreach (var teacher in model.Teachers)
                        {
                            var newTeacher = new Registerdetail
                            {
                                h_id = headId,
                                no = not,
                                Prefix = teacher.Prefix,
                                FirstName = teacher.FirstName,
                                LastName = teacher.LastName,
                                ImageUrl = teacher.ImageUrl,
                                Type = "teacher",
                                u_id = user.Id,
                                c_id = teacher.c_id,
                                lastupdate = DateTime.Now
                            }; // (โค้ดเดิมของคุณ)
                            _context.Registerdetail.Add(newTeacher);
                            not += 1;
                        }
                    }

                    msg += " เวลา ..."; // (โค้ด msg เดิมของคุณ)

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync(); // ⬅️ (แก้บั๊ก 3)
                    await LogAudit(user.Id, schoolName, "SaveStudents", true, msg);

                    // (Log)
                    // await _LineAlert.lineNotify(msg, ...);

                    // (สร้าง URL ปลายทาง)
                    string redirectUrl = User.IsInRole("Member")
                        ? Url.Action("frmRegister", "Member", new { c_id = competitionId })
                        : Url.Action("frmmanagerscore", "Manager", new { c_id = competitionId });

                    return Json(new
                    {
                        success = true,
                        message = "บันทึกข้อมูลเรียบร้อย!",
                        redirectUrl = redirectUrl // ⬅️ (ส่ง URL กลับไป)
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // ⬅️ (แก้บั๊ก 3)
                    await LogAudit(user.Id, schoolName, "SaveStudents", false, $"Exception: {ex.Message}"); // (ส่ง Error กลับไปเป็น Json)
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }
        }
        // (ฟังก์ชันช่วยสำหรับบันทึก Log)
        private async Task LogAudit(string userId, string schoolName, string action, bool success, string message)
        {
            // (ป้องกันกรณี SaveChanges ล้มเหลว และ Audit ก็ล้มเหลวตาม)
            try
            {
                var log = new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    SchoolName = schoolName,
                    Action = action,
                    Success = success,
                    Message = message
                };
                // (เราใช้ Context ใหม่ หรือ ปิด AsNoTracking)
                // (แต่วิธีที่ง่ายที่สุดคือ SaveChanges แยก)
                _context.AuditLog.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "!!! CRITICAL: FAILED TO WRITE AUDIT LOG !!!");
            }
        }
        public async Task<IActionResult> frmmanagerpic(int id, string c_name)
        {
            ViewBag.c_name = c_name;
            ViewBag.h_id = id;
            var data = await _context.Registerdetail.Where(x => x.h_id == id).ToListAsync();
            return View(data.OrderBy(x => x.Type));
        }
        [HttpPost]
        public async Task<IActionResult> UpdateImage(string croppedImage, int h_id, int no, string Type)
        {
            // 1. ดึงข้อมูลเก่ามาก่อน เพื่อเอารูปเก่าไปลบ
            var record = await _context.Registerdetail
                .Where(x => x.h_id == h_id && x.no == no && x.Type == Type)
                .FirstOrDefaultAsync();

            if (record != null)
            {
                // 2. ลบไฟล์รูปเก่าทิ้ง (ถ้ามี)
                try
                {
                    // 1. ดึงเฉพาะ "ชื่อไฟล์" ออกมาจาก URL (ไม่สนว่า URL จะยาวแค่ไหน)
                    // ตัวอย่าง: "/images/profile.jpg" -> ได้ "profile.jpg"
                    string fileName = Path.GetFileName(record.ImageUrl);

                    // 2. ระบุโฟลเดอร์ที่เก็บไฟล์ (images) โดยตรง เพื่อความแม่นยำ
                    // Path จะเป็น: .../wwwroot/images/profile.jpg
                    var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                    // --- DEBUG: ดูค่า Path ในหน้าต่าง Output ของ Visual Studio ---
                    Console.WriteLine($"[DEBUG] Trying to delete: {absolutePath}");
                    // -----------------------------------------------------------

                    if (System.IO.File.Exists(absolutePath))
                    {
                        System.IO.File.Delete(absolutePath);
                    }
                }
                catch (Exception ex)
                {
                    // ควร Log error ไว้ถ้าจำเป็น
                    Console.WriteLine($"[ERROR] Delete failed: {ex.Message}");
                }

                // 3. บันทึกรูปใหม่ (ชื่อไฟล์จะเปลี่ยนไปตามเวลา)
                var newImageUrl = await SaveCroppedImage(croppedImage, h_id, no, Type);

                // 4. อัปเดตฐานข้อมูล
                record.ImageUrl = newImageUrl;
                _context.Registerdetail.Update(record);
                // ใช้ SaveChanges ปกติเพื่อให้มั่นใจว่า Entity State ถูกอัปเดต
                await _context.SaveChangesAsync();

                return Json(new { success = true, newImageUrl });
            }

            return Json(new { success = false });
        }

        private async Task<string> SaveCroppedImage(string base64Image, int h_id, int no, string type)
        {
            var imageBytes = Convert.FromBase64String(base64Image.Split(',')[1]);

            // ✅ เพิ่ม DateTime.Now.Ticks เพื่อให้ชื่อไฟล์ไม่ซ้ำกันเลยในแต่ละครั้ง
            var fileName = $"image_{h_id}_{no}_{type}_{DateTime.Now.Ticks}.jpg";

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/images/{fileName}";
        }
        // (ใน MemberController.cs - Action frmschoolregister)

        public async Task<IActionResult> frmschoolregister(int c_id)
        {
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name", c_id);
            ViewBag.currentTypelevel = c_id;

            var user = await _userManager.GetUserAsync(User);

            // 1. ดึง ID งานปัจจุบัน (ตรวจสอบ null ด้วย)
            var activeSettingId = await _context.setupsystem
                .Where(s => s.status == "1")
                .Select(s => s.id)
                .FirstOrDefaultAsync();

            if (activeSettingId == 0)
            {
                // (จัดการกรณีไม่มีงาน Active)
                return View(new List<Registerhead>());
            }

            // 2. สร้าง Query หลัก (กรองที่ Root ก่อน)
            var query = _context.Registerhead
                .AsNoTracking()
                .Where(rh =>
                    rh.s_id == user.s_id &&       // 1. ของโรงเรียนนี้
                    rh.status == "1" &&           // 2. สถานะปกติ
                    rh.SettingID == activeSettingId // 3. ของงานปัจจุบัน
                );

            // 3. กรองตามหมวดหมู่ (c_id) ถ้ามี
            if (c_id != 0)
            {
                // (ต้องกรองผ่าน Navigation Property)
                query = query.Where(rh => rh.Competitionlist.c_id == c_id);
            }

            // 4. Include ข้อมูลที่จำเป็น (กรอง racedetails ใน Include ด้วย)
            var data = await query
                .Include(rh => rh.Registerdetail)
                .Include(rh => rh.Competitionlist)
                    .ThenInclude(cl => cl.Category)
                // (สำคัญ) Include racedetails เฉพาะของงานปัจจุบัน
                .Include(rh => rh.Competitionlist)
                    .ThenInclude(cl => cl.racedetails.Where(rd => rd.SettingID == activeSettingId && rd.status == "1"))
                        .ThenInclude(rd => rd.Racelocation)
                .OrderBy(rh => rh.id)
                .ToListAsync();

            return View(data);
        }
        public async Task<IActionResult> frmprintregister(int c_id, List<string> dateCheckboxes)
        {
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = c_id;




            var setting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            string racedate = setting.racedate; // มีช่องว่างรอบเครื่องหมาย '-'
            var formats = new[] { "MM/dd/yyyy", "MM/dd/yyyy" }; // รองรับหลายรูปแบบ
            // ลบช่องว่างรอบเครื่องหมาย '-'
            racedate = racedate.Replace(" ", "");
            string[] racestart = racedate.Split('-');
            // ตั้งค่า CultureInfo เพื่อแปลงวันที่จาก BE (พุทธศักราช)
            CultureInfo thaiCulture = new CultureInfo("th-TH");
            if (DateTime.TryParseExact(racestart[0], formats, thaiCulture, DateTimeStyles.None, out DateTime startDate) &&
                DateTime.TryParseExact(racestart[1], formats, thaiCulture, DateTimeStyles.None, out DateTime endDate))
            {
                // แปลงปีพุทธศักราช (BE) เป็นคริสต์ศักราช (CE)
                if (startDate.Year > 2500)
                {
                    startDate = startDate.AddYears(-543);
                }
                if (endDate.Year > 2500)
                {
                    endDate = endDate.AddYears(-543);
                }



                // สร้างรายการวันที่
                var dates = new List<DateTime>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    dates.Add(date);
                }

                // แสดงผลวันที่
                foreach (var date in dates)
                {
                    Console.WriteLine(date.ToString("dd/MM/yyyy")); // รูปแบบแสดงวันที่
                }
                ViewBag.DateList = dates;
            }
            else
            {
                Console.WriteLine("ไม่สามารถแปลงวันที่ได้");
            }
            var user = await _userManager.GetUserAsync(User);
            ViewBag.s_id = user.s_id;
            if (c_id != 0)
            {
                return RedirectToAction("GenePdfregister", "Pdfmember", new { s_id = user.s_id, dateCheckboxes = dateCheckboxes });
            }

            return View();
        }
        public async Task<IActionResult> frmSummaryresult(int c_id)
        {
            var user = await _userManager.GetUserAsync(User);
            var setting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            var activeSettingIds = await _context.setupsystem
    .Where(s => s.status == "1")
    .Select(s => s.id)
    .ToListAsync();
            ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            ViewBag.currentTypelevel = c_id;
            ViewBag.s_id = user.s_id;
            ViewBag.settingId = setting.id;
            var registerHeads = await _context.Registerhead.Where(x => x.status == "2" && x.s_id == user.s_id && activeSettingIds.Contains(x.SettingID)).AsNoTracking().ToListAsync();
            var query = _context.Registerhead
            .AsNoTracking()
     .Where(x => x.status == "2" && x.s_id == user.s_id && activeSettingIds.Contains(x.SettingID));

            // Add the competition filter only if c_id is not zero
            if (c_id != 0)
            {
                query = query.Where(x => x.Competitionlist.c_id == c_id);
            }

            // Project the results
            var data = await query
                .Select(x => new
                {
                    sid = x.s_id,
                    Id = x.id,
                    SettingId = x.SettingID,
                    Rank = x.rank,
                    Award = x.award,
                    RankDescription = x.rank == 1 ? "ชนะเลิศ" :
                                      x.rank == 2 ? "รองชนะเลิศอันดับ 1" :
                                      x.rank == 3 ? "รองชนะเลิศอันดับ 2" : $"{x.rank}",
                    Competitionlistname = x.Competitionlist.Name
                })
                .OrderBy(x => x.Rank)
                .ToListAsync();

            ViewBag.data = data;
            // ดึงข้อมูลทั้งหมดจาก Registerhead


            // คำนวณจำนวนทั้งหมด
            var totalCount = registerHeads.Count;

            // คำนวณจำนวนและเปอร์เซ็นต์ของแต่ละประเภทเหรียญ
            var goldCount = registerHeads.Count(r => r.award == "เหรียญทอง");
            var silverCount = registerHeads.Count(r => r.award == "เหรียญเงิน");
            var bronzeCount = registerHeads.Count(r => r.award == "เหรียญทองแดง");
            var participationCount = registerHeads.Count(r => r.award == "เข้าร่วม");

            var goldPercentage = ((double)goldCount / totalCount * 100).ToString("0.00");
            var silverPercentage = ((double)silverCount / totalCount * 100).ToString("0.00");
            var bronzePercentage = ((double)bronzeCount / totalCount * 100).ToString("0.00");
            var participationPercentage = ((double)participationCount / totalCount * 100).ToString("0.00");

            // สร้างออบเจ็กต์สำหรับส่งไปยัง View
            var awardSummary = new
            {
                Gold = new { Count = goldCount, Percentage = goldPercentage },
                Silver = new { Count = silverCount, Percentage = silverPercentage },
                Bronze = new { Count = bronzeCount, Percentage = bronzePercentage },
                Participation = new { Count = participationCount, Percentage = participationPercentage }
            };

            // ส่งข้อมูลไปยัง View
            ViewBag.AwardSummary = awardSummary;
            return View();
        }
    }
}