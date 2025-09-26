using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Silapa.Models;
using Syncfusion.EJ2.Linq;
using X.PagedList.Extensions;
using System.IO;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System;
using System.Linq;
namespace Silapa.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ResultsHub> _hubContext;
    System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext connectDbContext, IHubContext<ResultsHub> hubContext)
    {
        _logger = logger;
        _context = connectDbContext;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> IndexAsync()
    {
        //dการเข้าชมเว็บ
        var today = DateTime.Today;
        var weekOfYear = GetWeekOfYear(today);
        var month = today.Month;
        var year = today.Year;

        // เช็คและเพิ่มจำนวนการเข้าชมตามวัน
        await UpdateVisitCount(today.ToString(), VisitType.Day);

        // เช็คและเพิ่มจำนวนการเข้าชมตามสัปดาห์
        await UpdateVisitCount(weekOfYear.ToString(), VisitType.Week);

        // เช็คและเพิ่มจำนวนการเข้าชมตามเดือน
        await UpdateVisitCount(month.ToString(), VisitType.Month);

        // เช็คและเพิ่มจำนวนการเข้าชมตามปี
        await UpdateVisitCount(year.ToString(), VisitType.Year);


        var dailyStats = _context.VisitorCounts
                            .Where(vc => vc.VisitDate == DateTime.Today)
                            .FirstOrDefault();

        var weeklyStats = _context.VisitorCounts
    .AsEnumerable()
    .Where(vc => vc.Week == GetWeekOfYear(DateTime.Today))
    .FirstOrDefault();

        var monthlyStats = _context.VisitorCounts
                                   .Where(vc => vc.Month == DateTime.Today.Month)
                                   .FirstOrDefault();

        var yearlyStats = _context.VisitorCounts
                                  .Where(vc => vc.Year == DateTime.Today.Year)
                                  .FirstOrDefault();

        ViewBag.DailyVisits = dailyStats?.VisitCount ?? 0;
        ViewBag.WeeklyVisits = weeklyStats?.VisitCount ?? 0;
        ViewBag.MonthlyVisits = monthlyStats?.VisitCount ?? 0;
        ViewBag.YearlyVisits = yearlyStats?.VisitCount ?? 0;


        var registerDetails = await _context.Registerhead
    .Where(x => x.status != "0")
    .SelectMany(x => x.Registerdetail)
    .AsNoTracking()
    .ToListAsync();

        ViewBag.datacounts = registerDetails.Count(rd => rd.Type == "student");
        ViewBag.datacountt = registerDetails.Count(rd => rd.Type == "teacher");
        var competitionData = await _context.Competitionlist
     .AsNoTracking()
     .Where(x => x.status == "1")
     .ToListAsync();
        ViewBag.datalist = competitionData.Count;
        ViewBag.competitionData = competitionData;
        ViewBag.registerdirector = _context.registerdirector.AsNoTracking().Count(x => x.status == "1");
        ViewBag.refess = _context.referee.Where(x => x.status == "1").AsNoTracking().Count();
        var data = _context.news
                         .Where(x => x.status == "1")
                         .OrderByDescending(x => x.lastupdate) // เรียงลำดับจากวันที่ล่าสุดสุด
                         .Take(12) // ดึงเฉพาะ 3 รายการแรก
                         .ToList();
        ViewBag.news = data;
        ViewBag.School = _context.school.AsNoTracking().Count();
        ViewBag.datacategory = await _context.category.AsNoTracking().Where(x => x.status == "1").ToListAsync();
        ViewBag.datarace = await _context.racedetails.Include(x => x.Racelocation).AsNoTracking().ToListAsync();
        var countResults = await _context.Registerhead
    .Where(x => x.status == "2")
    .AsNoTracking()
    .GroupBy(x => x.c_id)  // จัดกลุ่มตาม c_id
    .CountAsync();         // นับจำนวนกลุ่ม

        ViewBag.countresults = countResults;
        // ViewBag.competition=
        return View();
    }
    public enum VisitType
    {
        Day,
        Week,
        Month,
        Year
    }

    private async Task UpdateVisitCount(string period, VisitType visitType)
    {
        var visitorCount = await _context.VisitorCounts
            .FirstOrDefaultAsync(vc =>
                (visitType == VisitType.Day && vc.VisitDate == DateTime.Today) ||
                (visitType == VisitType.Week && vc.Week == int.Parse(period)) ||
                (visitType == VisitType.Month && vc.Month == int.Parse(period)) ||
                (visitType == VisitType.Year && vc.Year == int.Parse(period))
            );

        if (visitorCount == null)
        {
            visitorCount = new VisitorCounts
            {
                VisitDate = visitType == VisitType.Day ? DateTime.Today : DateTime.MinValue, // set to today's date only for Day
                Week = visitType == VisitType.Week ? int.Parse(period) : 0,
                Month = visitType == VisitType.Month ? int.Parse(period) : 0,
                Year = visitType == VisitType.Year ? int.Parse(period) : 0,
                VisitCount = 1
            };
            _context.VisitorCounts.Add(visitorCount);
        }
        else
        {
            visitorCount.VisitCount += 1;
            _context.VisitorCounts.Update(visitorCount);
        }
        await _context.SaveChangesAsync();
    }
    // ใช้ฟังก์ชั่นนี้เพื่อคำนวณสัปดาห์ในปี
    private int GetWeekOfYear(DateTime date)
    {
        var culture = CultureInfo.InvariantCulture;
        var calendar = culture.Calendar;
        var dfi = culture.DateTimeFormat;
        return calendar.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
    }
    public async Task<IActionResult> frmcompetitionshow(int id, int c_id)
    {
        ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
        ViewBag.currentTypelevel = c_id;
        ViewBag.competition = new SelectList(_context.Competitionlist.Where(x => x.c_id == c_id).ToList(), "Id", "name");

        var data = await _context.Registerhead.Where(x => x.c_id == id).Include(x => x.Competitionlist).Include(x => x.Registerdetail).ToListAsync();
        if (data.Count > 0)
        {
            ViewBag.racedetails = _context.racedetails.Where(x => x.c_id == c_id).FirstOrDefault();
        }
        return View(data.OrderBy(x => x.id));
    }
    public async Task<IActionResult> frmshowlist()
    {
        var data = await _context.category.Where(x => x.status == "1").ToListAsync();
        return View(data.OrderBy(x => x.Id));
    }
    public async Task<IActionResult> contacts()
    {
        var data = await _context.contacts.Where(x => x.status == "1").ToListAsync();
        return View(data.OrderBy(x => x.id));
    }
    public async Task<IActionResult> frmshowlistdata(int id, string c_name)
    {
        ViewBag.c_name = c_name;
        var data = await _context.Competitionlist.Where(x => x.c_id == id && x.status == "1").ToListAsync();
        ViewBag.data = await _context.Registerhead.Where(x => x.status == "1").ToListAsync();
        return View(data.OrderBy(x => x.Id));
    }
    public async Task<IActionResult> frmshowlistdataAll(int id, string name)
    {
        ViewBag.name = name;
        var data = await _context.Registerhead.Where(x => x.c_id == id && x.status == "1").Include(x => x.Registerdetail).Include(x => x.School).Include(x => x.Competitionlist).ThenInclude(x => x.racedetails).ThenInclude(x => x.Racelocation).ToListAsync();
        return View(data.OrderBy(x => x.id));
    }
    public async Task<IActionResult> frmnewsshow(int id)
    {
        return View(await _context.news.FindAsync(id));
    }
    [HttpGet]
    public JsonResult GetCompetitions(int c_id)
    {
        var competitions = _context.Competitionlist
            .Where(c => c.c_id == c_id && c.status == "1") // Assuming `CategoryId` matches `c_id`
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();

        return Json(competitions);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public IActionResult ListRegister()
    {
        // var data=_context.ToList();
        //ViewBag.dataSource=data;
        return View();
    }
    public IActionResult frmList()
    {
        return View();
    }
    public IActionResult frmnewsAll(int? page)
    {
        int pageSize = 6; // จำนวนรายการต่อหน้า
        int pageNumber = page ?? 1; // หน้าปัจจุบัน ถ้าไม่มีให้เริ่มที่หน้า 1
        var data = _context.news.Where(x => x.status == "1").OrderByDescending(n => n.lastupdate).ToPagedList(pageNumber, pageSize); ;
        return View(data);
    }
    public IActionResult frmregisterdirector()
    {
        DateTime todate = DateTime.Now; // วันที่ปัจจุบัน
        DateTime endDateNow = new DateTime(Convert.ToInt16(2567), Convert.ToInt16(11),
                 Convert.ToInt16(12));
        if (endDateNow.Year > 2500)
        {
            endDateNow = endDateNow.AddYears(-543);
        }
        if (todate > endDateNow)
        {
            return RedirectToAction("index", "Home");
        }
        ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
        ViewBag.currentTypelevel = 0;
        return View(new registerdirector());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> frmregisterdirector(registerdirector data, IFormFile ProfileImage)
    {

        // ModelState.Remove("ProfileImageUrl");
        ModelState.Remove("id");

        // ModelState.Remove("c_id");
        //ModelState.Remove("g_id");
        // ModelState.Remove("ProfileImage");
        //ModelState.Remove("tel");
        //ModelState.Remove("name");
        //ModelState.Remove("ProfileImage");
        //ModelState.Remove("lastupdate");
        // ModelState.Remove("experience");

        //  ModelState.Remove("ProfileImage");
        if (ModelState.IsValid)
        {
            // ตรวจสอบว่ามีการเลือกรูปภาพแล้วหรือไม่
            if (ProfileImage == null || ProfileImage.Length == 0)
            {
                ModelState.AddModelError("ProfileImageUrl", "กรุณาเลือกรูปภาพก่อนบันทึกข้อมูล");
                ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
                ViewBag.currentTypelevel = data.g_id;
                return View(data); // ส่งกลับไปที่ฟอร์มพร้อมกับข้อความแจ้งเตือน
            }
            if (data.c_id == 0)
            {
                ModelState.AddModelError("c_id", "กรุณาเลือกรายการแข่งขัน");
                ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
                ViewBag.currentTypelevel = data.g_id;
                return View(data); // ส่งกลับไปที่ฟอร์มพร้อมกับข้อความแจ้งเตือน
            }
            if (ProfileImage.Length > 5 * 1024 * 1024)  // 5MB limit
            {
                ModelState.AddModelError("ProfileImage", "ขนาดไฟล์รูปภาพต้องไม่เกิน 5MB");
                ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
                ViewBag.currentTypelevel = data.g_id;
                return View(data);
            }
            // การอัปโหลดและเปลี่ยนชื่อรูปภาพ
            if (ProfileImage != null)
            {
                // สร้างชื่อไฟล์ใหม่โดยใช้ GUID และนามสกุลเดิมของไฟล์
                var fileExtension = Path.GetExtension(ProfileImage.FileName);
                var newFileName = $"{Guid.NewGuid()}{fileExtension}";

                var filePath = Path.Combine("wwwroot/images", newFileName);

                // ลดขนาดรูปภาพก่อนบันทึก
                using (var stream = new MemoryStream())
                {
                    await ProfileImage.CopyToAsync(stream);
                    stream.Position = 0; // รีเซ็ตตำแหน่งของ stream ไปที่จุดเริ่มต้น

                    try
                    {
                        using (var image = Image.Load(stream))
                        {
                            // กำหนดขนาดใหม่ที่ต้องการ
                            int newWidth = 256;
                            int newHeight = (int)(image.Height * newWidth / image.Width);

                            // Resize รูปภาพ
                            image.Mutate(x => x.Resize(newWidth, newHeight));

                            // บันทึกรูปภาพที่ถูก resize
                            image.Save(filePath, new JpegEncoder());
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ProfileImage", "รูปภาพไม่สามารถโหลดได้, โปรดตรวจสอบไฟล์ภาพ");
                        ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
                        ViewBag.currentTypelevel = data.g_id;
                        return View(data);
                    }

                }

                data.lastupdate = DateTime.Now;

                data.ProfileImageUrl = $"/images/{newFileName}";
            }
            data.lastupdate = DateTime.Now;
            data.status = "1";
            _context.registerdirector.Add(data);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
        ViewBag.currentTypelevel = data.g_id;
        return View(data);
    }
    public JsonResult GetItemsByCategory(int categoryId)
    {
        // ตัวอย่างการสร้างรายการตามหมวดหมู่
        var items = new List<SelectListItem>();

        var data = _context.Competitionlist.Where(x => x.c_id == categoryId && x.status == "1").ToList();
        foreach (var item in data)
        {
            items.Add(new SelectListItem { Value = item.Id.ToString(), Text = item.Name });
        }
        return Json(items);
    }
    public async Task<IActionResult> frmregisterdirectorAll()
    {
        var data = await _context.Competitionlist.Where(x => x.status == "1").ToListAsync();
        ViewBag.data = await _context.registerdirector.ToListAsync();
        return View(data.OrderBy(x => x.c_id));
    }
    public async Task<IActionResult> frmrefereeAll(int m_id)
    {
        var categories = _context.category.Where(c => c.status == "1").ToList();
        var datgroupreferee = await _context.groupreferee.ToListAsync();
        var datareferee = await _context.referee.Where(x => x.status == "1").ToListAsync();
        var dataCompetitionlist = await _context.Competitionlist.Where(x => x.status == "1").ToListAsync();

        if (m_id != 0)
        {
            datgroupreferee = datgroupreferee.Where(x => x.c_id == m_id).ToList();
            datareferee = datareferee.Where(x => x.m_id == m_id || m_id == 31).ToList();
            dataCompetitionlist = dataCompetitionlist.Where(x => x.c_id == m_id).ToList();
        }
        ViewBag.referee31 = _context.referee.Where(x => x.m_id == 31).ToList();
        ViewBag.com = dataCompetitionlist;
        ViewBag.groupreferee = datgroupreferee;
        ViewBag.referee = datareferee;
        ViewBag.categoryData = new SelectList(categories, "Id", "Name", m_id);

        return View(categories);
    }
    public async Task<IActionResult> frmschoolAll()
    {
        var registrationBySchool = _context.school
    .Where(x => x.status == "1") // โรงเรียนที่เปิดใช้งาน
    .Select(school => new
    {
        SchoolId = school.Id,
        SchoolName = school.Name,
        TotalRegistrations = _context.Registerhead
            .Count(rh => rh.s_id == school.Id && rh.status != "0"), // จำนวนการลงทะเบียนที่ status == "1" ในโรงเรียนนี้
        TotalStudents = _context.Registerdetail
            .Count(rd => _context.Registerhead
                .Any(rh => rh.id == rd.h_id && rh.s_id == school.Id && rh.status != "0") && rd.Type == "student"), // ยอดรวมนักเรียนในโรงเรียนที่ลงทะเบียนสถานะ "1"
        TotalTeachers = _context.Registerdetail
            .Count(rd => _context.Registerhead
                .Any(rh => rh.id == rd.h_id && rh.s_id == school.Id && rh.status != "0") && rd.Type == "teacher") // ยอดรวมครูในโรงเรียนที่ลงทะเบียนสถานะ "1"
    })
    .ToList();
        return View(registrationBySchool);
    }
    public async Task<IActionResult> frmschoolDetail(int id, string name)
    {
        ViewBag.id = id;
        ViewBag.name = name;
        ViewBag.Competitionlist = await _context.Competitionlist.Where(x => x.status == "1").Include(x => x.racedetails).ThenInclude(x => x.Racelocation).ToListAsync();
        var data = await _context.Registerhead.Where(x => x.status == "1" && x.s_id == id).Include(x => x.Registerdetail).Include(x => x.Competitionlist).ThenInclude(x => x.racedetails).ThenInclude(x => x.Racelocation).ToListAsync();
        ViewBag.StudentCount = data.Sum(x => x.Registerdetail.Count(detail => detail.Type == "student"));
        ViewBag.TeacherCount = data.Sum(x => x.Registerdetail.Count(detail => detail.Type == "teacher"));
        return View(data);
    }
    public async Task<IActionResult> frmcriterion()
    {
        var data = await _context.criterion.Where(x => x.status == "1").ToListAsync();
        return View(data.OrderBy(x => x.id));
    }
    public async Task<IActionResult> frmfilelistAll()
    {
        var data = await _context.filelist.Where(x => x.status == "1").ToListAsync();
        return View(data.OrderBy(x => x.id));
    }
    public async Task<IActionResult> frmresults(int c_id)
    {
        var data = await _context.setupsystem.FirstOrDefaultAsync();

        ViewBag.setupsystem = data;
        ViewBag.levelData = new SelectList(_context.category.Where(x => x.status == "1").ToList(), "Id", "Name");
        ViewBag.currentTypelevel = c_id;
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetCertificateDate(int settingId)
    {
        // ดึงข้อมูลจากฐานข้อมูล
    var dataQuery = _context.setupsystem
        .Where(x => x.status == "1"); // เงื่อนไขเริ่มต้น

    // ตรวจสอบว่า settingId มีค่าและกรองข้อมูล
    if (settingId != 0)
    {
        dataQuery = dataQuery.Where(x => x.id == settingId);
    }

    // ดึงข้อมูลรายการแรกที่ตรงกับเงื่อนไข
    var data = await dataQuery.FirstOrDefaultAsync();

    // ตรวจสอบว่าพบข้อมูลหรือไม่
    if (data == null)
    {
        return Json(new { success = false, message = "ไม่พบข้อมูลสำหรับ Setting ID นี้" });
    }

    // คืนค่า JSON พร้อมวันที่ในรูปแบบที่ต้องการ
    return Json(new { success = true, certificatedate = data.certificatedate });
    }
    public IActionResult GetCompetitionsresult(int c_id)
    {
        // ดึงข้อมูลรายการแข่งขันจากฐานข้อมูล
        var competitions = _context.Competitionlist
     .Where(c => c.c_id == c_id && c.registerheads.Any(r => r.status == "2"))
     .GroupBy(c => new { c.Id, c.Name }) // กลุ่มตาม Id และ Name
     .Select(g => new
     {
         id = g.Key.Id,
         name = g.Key.Name
     })
     .ToList();

        return Json(competitions);
    }
    public IActionResult GetResults(int competitionId)
    {
        var results = _context.Registerhead
        .Include(x => x.School)
            .Where(r => r.c_id == competitionId && r.status == "2")
            .Select(r => new
            {
                id = r.id,
                school = r.School.Name,
                CompetitionName = r.Competitionlist.Name,
                score = r.score,
                level = r.rank, // Function to determine level based on score
                notes = r.award
            })
            .OrderByDescending(r => r.score) // Optional: Order by score
            .ToList();

        return Json(results);
    }

    public async Task<IActionResult> frmtestsendmsg()
    {
        return View();
    }
    public async Task<IActionResult> frmAnnouncement()
    {
        // ดึงข้อมูลผลการแข่งขันจากฐานข้อมูล
        // ดึงข้อมูลผลการแข่งขันจากฐานข้อมูล
        var results = await _context.Competitionlist
            .Include(x => x.Category) // โหลดข้อมูล Category
            .GroupBy(x => new { x.Category.Id, x.Category.Name }) // จัดกลุ่มตาม Category
            .Select(g => new ResultGroupViewModel
            {
                Code = g.Key.Id.ToString(), // ใช้ Id ของ Category เป็น Code
                Name = g.Key.Name, // ใช้ Name ของ Category เป็นหัวข้อ
                Results = g.Select(r => new ResultViewModel
                {
                    Order = r.Id, // ใช้ Id ของการแข่งขันเป็นลำดับ
                    Status = r.status ?? "รอผล", // ถ้า status เป็น null, ใช้ค่าเริ่มต้น
                    Description = r.Name // ชื่อการแข่งขัน
                }).ToList()
            }).ToListAsync();

        // ส่งข้อมูลผ่าน SignalR
        await _hubContext.Clients.All.SendAsync("UpdateResults", results);

        return View();
    }
    public IActionResult GetPersonDetails(string query)
    {
        // ตัวอย่างข้อมูลที่ดึงมาจากฐานข้อมูล
        string searchQuery = query;
        var nameParts = searchQuery.Split(' ');

        var data = _context.Registerhead
            .Include(x => x.Registerdetail)
            .Include(x => x.School)
            .Include(x => x.Competitionlist)
            .ThenInclude(x => x.racedetails)
            .ThenInclude(x => x.Racelocation)
            .Where(x => x.Registerdetail.Any(rd =>
                nameParts.Length == 2 &&
                rd.FirstName.Contains(nameParts[0]) &&
                rd.LastName.Contains(nameParts[1])))
            .ToList();


        // สร้าง HTML เพื่อตอบกลับไปยัง JavaScript
        var htmlContent = new StringBuilder();
        foreach (var item in data)
        {
            foreach (var detail in item.Registerdetail.Where(x => x.FirstName == nameParts[0] && x.LastName == nameParts[1]))
            {
                htmlContent.Append($@"
            <div class='card card-primary card-outline'>
              <div class='card-body box-profile'>
                <div class='text-center'>
                  <img class='profile-user-img img-fluid img-circle'
                       src='{detail.ImageUrl}'
                       alt='User profile picture'>
                </div >
                <h3 class='profile-username text-center'>{detail.Prefix}{detail.FirstName} {detail.LastName}</h3>
                <p class='text-muted text-center'>{item.School.Name}</p>
                <ul class='list-group list-group-unbordered mb-3'>
                  <li class='list-group-item'>
                    <b>รายการ:{item.Competitionlist.Name}</b> 
                  </li>
                  <li class='list-group-item'>
                   <b>{GetCompetitionDetails(item.c_id, thaiCulture)}
                   </b>
                  </li>
                  <li class='list-group-item'>
                    <b>รายละเอียด:{item.Competitionlist?.racedetails?.FirstOrDefault()?.details ?? "ไม่มีข้อมูล"}</b> 
                  </li>
                </ul>
              </div>
            </div>");
            }
        }

        var datar = _context.referee
        .Include(x => x.Competitionlist)
        .Where(x => x.name == searchQuery)
        .FirstOrDefault();
        if (datar != null)
        {
            var role = "";
            if (datar.c_id == 0)
            {
                role = "กรรมการดำเนินการ";
            }
            else
            {
                role = datar.Competitionlist.Name ?? "ไม่มีข้อมูล";
            }

            htmlContent.Append($@"
            <div class='card card-primary card-outline'>
              <div class='card-body box-profile'>
                <div class='text-center'>
                  <img class='profile-user-img img-fluid img-circle'
                       src='{datar.ImageUrl}'
                       alt='User profile picture'>
                </div >
                <h3 class='profile-username text-center'>{datar.name}</h3>
                <p class='text-muted text-center'>{datar.position}</p>
                <ul class='list-group list-group-unbordered mb-3'>
                  <li class='list-group-item'>
                    <b>กรรมการ:{role}</b> 
                  </ li >
                  <li class='list-group-item'>
                   <b>{GetCompetitionDetails((int)datar.c_id, thaiCulture)}
                   </b>
                  </li>
                </ ul >
              </ div >
            </ div > ");
        }


        ///

        return Content(htmlContent.ToString(), "text/html");
    }
    public async Task<IActionResult> genshowlistrefect()
    {
        var targetDates = new List<string>
{
    "12/12/2024 - 12/12/2024",
    "12/13/2024 - 12/13/2024",
    "12/14/2024 - 12/14/2024"
};

        var categories = new Dictionary<int, string>
{
    { 12, "ภาษาไทย" },
    { 13, "คณิตศาสตร์" },
    { 14, "วิทยาศาสตร์" },
    { 16, "สุขศึกษาและพละศึกษา" },
    { 1, "สังคม" },
    { 22, "ภาษาต่างประเทศ" },
    { 15, "คอมพิวเตอร์" },
    { 4, "สุขศึกษาและพละศึกษา" },
    { 17, "สุขศึกษาและพละศึกษา" },
    { 5, "ทัศนศิลป์" },
    { 6, "นาฏศิลป์" },
    { 7, "การงานอาชีพ" },
    { 19, "การงานอาชีพ" },
    { 3, "พัฒนาผู้เรียน" },
    { 23, "พัฒนาผู้เรียน" },
    { 24, "เรียมรวม" }
};

        var data = _context.Competitionlist
    .Include(x => x.Category)
    .Include(c => c.racedetails)
    .Include(c => c.referees)
    .Where(c => c.racedetails.Any(rd => targetDates.Contains(rd.daterace))) // Filter by target dates
    .AsEnumerable() // Bring data into memory
    .SelectMany(c => c.racedetails
        .Where(rd => targetDates.Contains(rd.daterace))
        .Select(rd => new
        {
            RaceDate = rd.daterace,
            CategoryId = c.Category.Id, // Category ID
            CategoryName = c.Category.Name, // Category Name
            OperationalReferees = c.referees
                .Where(r => r.c_id == 0 && r.m_id == rd.Competitionlist.c_id) // Operational referees
                .Select(r => r.name)
                .Distinct()
                .ToList(),
            JudgingReferees = c.referees
                .Where(r => r.c_id != 0 && r.m_id == rd.Competitionlist.c_id) // Judging referees
                .Select(r => r.name)
                .Distinct()
                .ToList()
        }))
    .GroupBy(r => r.CategoryId) // Group by Category ID
    .Select(g => new
    {
        CategoryId = g.Key,
        CategoryName = g.FirstOrDefault().CategoryName, // Get the category name
        Races = g.GroupBy(r => r.RaceDate) // Group by RaceDate within each Category
            .Select(rg => new
            {
                RaceDate = rg.Key,
                TotalOperationalCount = rg.SelectMany(r => r.OperationalReferees).Distinct().Count(), // Count unique operational referees
                TotalJudgingCount = rg.SelectMany(r => r.JudgingReferees).Distinct().Count(), // Count unique judging referees
                OperationalReferees = rg.SelectMany(r => r.OperationalReferees).Distinct().ToList(), // Get distinct operational referees
                JudgingReferees = rg.SelectMany(r => r.JudgingReferees).Distinct().ToList() // Get distinct judging referees
            })
            .ToList()
    })
    .ToList(); // Execute the query and get the data


        return PartialView("_PartialShowList", data);
    }
    public string GetCompetitionDetails(int c_id, CultureInfo thaiCulture)
    {
        var datadd = _context.racedetails.Where(x => x.c_id == c_id).FirstOrDefault();
        if (datadd == null)
        {
            return "ไม่มีข้อมูล"; // กรณีไม่มีข้อมูล
        }

        string dd = datadd.daterace ?? "";
        string[] ddsub;
        string startDateFormatted = "";
        string endDateFormatted = "";
        string time = datadd.time ?? "";
        string building = datadd.building ?? "";
        string room = datadd.room ?? "";
        string name = datadd.Racelocation?.name ?? "";

        // แยกวันที่
        ddsub = dd.Split('-');
        if (ddsub.Length == 2)
        {
            string[] startdate = ddsub[0].Split('/');
            string[] enddate = ddsub[1].Split('/');

            if (startdate.Length == 3 && enddate.Length == 3)
            {
                DateTime startDateNow = new DateTime(Convert.ToInt16(startdate[2]), Convert.ToInt16(startdate[0]), Convert.ToInt16(startdate[1]));
                DateTime endDateNow = new DateTime(Convert.ToInt16(enddate[2]), Convert.ToInt16(enddate[0]), Convert.ToInt16(enddate[1]));

                int buddhistYearS = startDateNow.Year + 543;
                int buddhistYearN = endDateNow.Year + 543;

                startDateFormatted = startDateNow.ToString("dd MMMM", thaiCulture) + " " + buddhistYearS;
                endDateFormatted = endDateNow.ToString("dd MMMM", thaiCulture) + " " + buddhistYearN;
            }
        }

        // รวมผลลัพธ์ทั้งหมดเป็นข้อความ
        return $"วันที่แข่งขัน: {startDateFormatted} - {endDateFormatted}\n" +
               $"เวลา: {time}\n" +
               $"อาคาร: {building}\n" +
               $"ห้อง: {room}\n" +
               $"สถานที่: {name}";
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
    public async Task<IActionResult> frmsearch(string searchInput)
    {
        var htmlContent = new StringBuilder();
        if (searchInput != null)
        {
            // ตัวอย่างข้อมูลที่ดึงมาจากฐานข้อมูล
            string searchQuery = searchInput;
            var nameParts = searchQuery.Split(' ');

            var data = _context.Registerhead
                .Include(x => x.Registerdetail)
                .Include(x => x.School)
                .Include(x => x.Competitionlist)
                .ThenInclude(x => x.racedetails)
                .ThenInclude(x => x.Racelocation)
                .Where(x => x.Registerdetail.Any(rd =>
                    nameParts.Length == 2 &&
                    rd.FirstName.Contains(nameParts[0]) &&
                    rd.LastName.Contains(nameParts[1])))
                .ToList();
            // สร้าง HTML เพื่อตอบกลับไปยัง JavaScript

            foreach (var item in data)
            {
                foreach (var detail in item.Registerdetail.Where(x => x.FirstName == nameParts[0] && x.LastName == nameParts[1]))
                {
                    htmlContent.Append($@"
            <div class='card card-primary card-outline'>
              <div class='card-body box-profile'>
                <div class='text-center'>
                  <img class='profile-user-img img-fluid img-circle'
                       src='{detail.ImageUrl}'
                       alt='User profile picture'>
                </div >
                <h3 class='profile-username text-center'>{detail.Prefix}{detail.FirstName} {detail.LastName}</h3>
                <p class='text-muted text-center'>{item.School.Name}</p>
                <ul class='list-group list-group-unbordered mb-3'>
                  <li class='list-group-item'>
                    <b>รายการ:{item.Competitionlist.Name}</b> 
                  </li>
                  <li class='list-group-item'>
                   <b>{GetCompetitionDetails(item.c_id, thaiCulture)}
                   </b>
                  </li>
                  <li class='list-group-item'>
                    <b>รายละเอียด:{item.Competitionlist?.racedetails?.FirstOrDefault()?.details ?? "ไม่มีข้อมูล"}</b> 
                  </li>
                </ul>
              </div>
            </div>");
                }
            }

            var datar = _context.referee
            .Include(x => x.Competitionlist)
            .Where(x => x.name == searchQuery)
            .FirstOrDefault();
            if (datar != null)
            {
                var role = "";
                if (datar.c_id == 0)
                {
                    role = "กรรมการดำเนินการ";
                }
                else
                {
                    role = datar.Competitionlist.Name ?? "ไม่มีข้อมูล";
                }

                htmlContent.Append($@"
            <div class='card card-primary card-outline'>
              <div class='card-body box-profile'>
                <div class='text-center'>
                  <img class='profile-user-img img-fluid img-circle'
                       src='{datar.ImageUrl}'
                       alt='User profile picture'>
                </div >
                <h3 class='profile-username text-center'>{datar.name}</h3>
                <p class='text-muted text-center'>{datar.position}</p>
                <ul class='list-group list-group-unbordered mb-3'>
                  <li class='list-group-item'>
                    <b>กรรมการ:{role}</b> 
                  </ li >
                  <li class='list-group-item'>
                   <b>{GetCompetitionDetails((int)datar.c_id, thaiCulture)}
                   </b>
                  </li>
                </ ul >
              </ div >
            </ div > ");
            }
        }

        ViewBag.name = htmlContent;
        return View();
    }
    public async Task<IActionResult> frmrsummarizeesults()
    {
        var medalSummary = await _context.Registerhead
         .AsNoTracking()
         .Where(r => r.status == "2")
         .GroupBy(r => r.School.Name)
         .Select(g => new
         {
             SchoolName = g.Key,
             GoldCount = g.Count(r => r.award == "เหรียญทอง"),
             SilverCount = g.Count(r => r.award == "เหรียญเงิน"),
             BronzeCount = g.Count(r => r.award == "เหรียญทองแดง"),
             ParticipationCount = g.Count(r => r.award == "เข้าร่วม"),
             TotalMedals = g.Count()
         })
         .OrderByDescending(g => g.GoldCount)
         .ThenByDescending(g => g.SilverCount)
         .ThenByDescending(g => g.BronzeCount)
         .ToListAsync();

        var numberSummary = await _context.Registerhead
        .AsNoTracking()
        .Where(r => r.status == "2")
        .GroupBy(r => r.School.Name)
        .Select(g => new
        {
            SchoolName = g.Key,
            number1 = g.Count(r => r.rank == 1),
            number2 = g.Count(r => r.rank == 2),
            number3 = g.Count(r => r.rank == 3)
        })
        .OrderByDescending(g => g.number1)
        .ThenByDescending(g => g.number2)
        .ThenByDescending(g => g.number3)
        .ToListAsync();
        /*
                var categoriessummary = await _context.Registerhead
                .AsNoTracking()
                .Where(x => x.Competitionlist.Category.status == "1")
            .GroupBy(r => r.Competitionlist.Category.Name)
            .Select(g => new
            {
                CategoryName = g.Key,
                RegistrationCount = g.Count(r => r.status == "2"),
                RegistrationCounttotal = g.Count(),
                NegativeScoreCount = g.Count(r => r.score == -1)
            })
            .ToListAsync();*/

        /*
                var competitionData = await _context.Registerhead
                .AsNoTracking()
                    .Where(x => x.status == "2")
                    .Include(rh => rh.Registerdetail)
                    .ThenInclude(rd => rd.Registerhead.Competitionlist)
                    .ThenInclude(cl => cl.Category)
                    .GroupBy(rh => rh.Competitionlist.Category.Name)
                    .Select(g => new
                    {
                        CategoryName = g.Key,
                        StudentCount = g.Sum(rh => rh.Registerdetail.Count(rd => rd.Type == "student")),
                        TeacherCount = g.Sum(rh => rh.Registerdetail.Count(rd => rd.Type == "teacher"))
                    })
                    .ToListAsync();*/


        ///ดึงข้อมูลโรงเรียนตามสังกัด
        ///
        /*
       var schoolDataByAffiliation = await _context.Registerhead
    .AsNoTracking()
    .Include(rh => rh.School) // โหลดข้อมูลโรงเรียน (รวมถึง a_id)
    .ThenInclude(s => s.Affiliation) // โหลดข้อมูลสังกัดที่สัมพันธ์กับโรงเรียน
    .Include(rh => rh.Registerdetail)
    .GroupBy(rh => new { rh.School.a_id }) // กลุ่มตาม a_id และ s_id
    .Select(g => new
    {
        AffiliationId = g.Key.a_id, // รหัสสังกัด
        AffiliationName = g.FirstOrDefault().School.Affiliation.Name, // ชื่อสังกัด
       // SchoolId = g.Key.s_id, // รหัสโรงเรียน
       // SchoolName = g.FirstOrDefault().School.Name, // ชื่อโรงเรียน
        SchoolCount = g.Select(rh => rh.s_id).Distinct().Count(),
        StudentCount = g.Sum(rh => rh.Registerdetail.Count(rd => rd.Type == "student")), // จำนวนนักเรียน
        TeacherCount = g.Sum(rh => rh.Registerdetail.Count(rd => rd.Type == "teacher")), // จำนวนครู
        RegistrationCount = g.Count() // จำนวนครั้งที่โรงเรียนลงทะเบียน
    })
    .OrderBy(g => g.AffiliationId) // เรียงตามรหัสสังกัด
    .ThenByDescending(g => g.StudentCount) // เรียงตามจำนวนนักเรียน
    .ToListAsync();*/
        // ดึงข้อมูลทั้งหมดจาก Registerhead
        /*
          var registerHeads = await _context.Registerhead.Where(x => x.status == "2").AsNoTracking().ToListAsync();

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
          ViewBag.AwardSummary = awardSummary;*/

        ViewBag.MedalSummary = medalSummary;
        ViewBag.numberSummary = numberSummary;
        // ViewBag.categoriessummary = categoriessummary;
        //  ViewBag.CompetitionData = competitionData;
        // ViewBag.schoolDataByAffiliation=schoolDataByAffiliation;
        var data = await _context.setupsystem.FirstOrDefaultAsync();
        ViewBag.setupsystem = data;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetSchoolDetails(string schoolName)
    {
        var ranks = new List<int?> { 1, 2, 3 };
        var details = await _context.Registerhead
            .Where(r => r.School.Name == schoolName && r.status == "2" && r.rank.HasValue && ranks.Contains(r.rank.Value))
            .Select(r => new
            {
                CompetitionName = r.Competitionlist.Name,
                Rank = r.rank,
                RankDescription = r.rank == 1 ? "ชนะเลิศ" :
                                  r.rank == 2 ? "รองชนะเลิศอันดับ 1" :
                                  r.rank == 3 ? "รองชนะเลิศอันดับ 2" : "อื่น ๆ"
            })
            .OrderBy(r => r.Rank)
            .ToListAsync();

        return PartialView("_SchoolDetails", details); // ใช้ PartialView
    }
    [HttpGet]
    public async Task<IActionResult> GetSchoolDetailsresult(int id)
    {
        var details = await _context.Registerhead
    .Where(r => r.id == id)
    .Select(r => new
    {
        H_id = r.id,
        settingId = r.SettingID,
        CompetitionName = r.Competitionlist.Name,
        Rank = r.rank,
        RankDescription = r.rank == 1 ? "ชนะเลิศ" :
                          r.rank == 2 ? "รองชนะเลิศอันดับ 1" :
                          r.rank == 3 ? "รองชนะเลิศอันดับ 2" : $"{r.rank}",
        Participants = r.Registerdetail.Select(d => new
        {
            FullName = $"{d.Prefix}{d.FirstName} {d.LastName}",
            d.ImageUrl,
            d.Type
        }).OrderBy(x => x.Type).ToList()
    })
    .FirstOrDefaultAsync();


        return PartialView("_SchoolDetailsresult", details); // ใช้ PartialView
    }
    [HttpGet]
    public IActionResult GetSummaryResults()
    {
        // ดึงข้อมูล Registerhead ที่ status = 1
        var results = _context.Registerhead
            .Where(rh => rh.status == "1" && rh.award != "ไม่ได้แข่งขัน") // กรองเฉพาะ status = 1
            .Join(
                _context.Competitionlist, // Join กับตาราง Competitionlist
                rh => rh.c_id,           // Foreign Key
                cl => cl.Id,             // Primary Key
                (rh, cl) => new { cl.Name } // ดึงเฉพาะชื่อ Competitionlist
            )
            .Distinct() // ลบรายการที่ซ้ำกัน
            .ToList();

        return Json(results); // ส่งผลลัพธ์ในรูปแบบ JSON
    }
    public async Task<IActionResult> frmexamine(string gencode, string search, int c_id, string type)
    {
        ViewBag.levelData = new SelectList(_context.setupsystem.ToList(), "id", "name");
        ViewBag.currentTypelevel = c_id;
        ViewBag.search = search;
        ViewBag.TypeOptions = new List<SelectListItem>
    {
        new SelectListItem { Text = "นักเรียน/ครู", Value = "1", Selected = true },
        new SelectListItem { Text = "กรรมการ", Value = "2" }
    };
        ViewBag.currentType = type;
        if (type == "1")
        {
            var query = _context.Registerdetail
            .Include(d => d.Registerhead) // ดึงความสัมพันธ์ไปยัง Registerhead
            .ThenInclude(r => r.Competitionlist) // ดึงข้อมูลจาก Competitionlist ผ่าน Registerhead
            .AsQueryable();
            if (c_id != 0)
            {
                query = query.Where(r => r.Registerhead.SettingID == c_id);
            }
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d =>
         (d.FirstName + " " + d.LastName).Contains(search) || // ค้นหาแบบชื่อเต็ม
         d.FirstName.Contains(search) ||                     // ค้นหาเฉพาะชื่อ
         d.LastName.Contains(search));                       // ค้นหาเฉพาะนามสกุล

            }

            var result = await query.Select(d => new
            {
                Id=d.h_id,
                Settingid=d.Registerhead.SettingID,
                SchoolName=d.Registerhead.School.Name,
                Fullname = d.Prefix + d.FirstName + " " + d.LastName, // ชื่อเต็ม
                ImageUrl = d.ImageUrl,                               // รูปภาพ
                RegistrationNo = d.no,                   // หมายเลขลงทะเบียน (ถ้าต้องการ)
                Lastupdate = d.lastupdate,
                CompetitionlistName = d.Registerhead.Competitionlist.Name,
                Award = d.Registerhead.award,
                Namejob = d.Registerhead.Setupsystem.name,
                Location=d.Registerhead.Competitionlist.racedetails.FirstOrDefault().Racelocation.name,
                RoleDescription = d.Type == "teacher"
            ? "เป็นครูผู้ฝึกสอนนักเรียน"  
                : "", // ค่าเริ่มต้นหากไม่ใช่ teacher หรือ student
                Rank = d.Registerhead.rank == 1 ? "ชนะเลิศ" :
               d.Registerhead.rank == 2 ? "รองชนะเลิศ อันดับ 1" :
               d.Registerhead.rank == 3 ? "รองชนะเลิศ อันดับ 2" :
               "",
            }).ToListAsync();

            // ส่งต่อข้อมูลไปยัง View
            ViewBag.Data = result;
            ViewBag.type = type;
        }
        else if (type == "2")
        {
            var query = _context.referee
            .Include(x => x.Groupreferee)
            .Include(x => x.Setupsystem)
            .AsQueryable();
            if (c_id != 0)
            {
                query = query.Where(r => r.SettingID == c_id);
            }
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => (d.name).Contains(search));
            }
            var result = await query.Select(d => new
            {
                Id=d.id,
                Settingid=d.SettingID,
                Fullname = d.name,
                ImageUrl = d.ImageUrl,
                Namejob = d.Setupsystem.name,
                SchoolName=d.role,
                RoleDescription = d.g_id == 0
        ? "กรรมการตัดสิน: " + _context.Competitionlist
            .Where(c => c.Id == d.c_id)
            .Select(c => c.Name)
            .FirstOrDefault() // ดึงชื่อรายการที่เกี่ยวข้อง
        : _context.groupreferee.Where(x => x.id == d.g_id)
            .Select(c=>c.name)
            .FirstOrDefault() , // ดึงหน้าที่จาก Groupreferee
            Category=d.c_id==0
            ? _context.category.Where(x=>x.Id==d.m_id)
            .Select(x=>x.fullname)
            .FirstOrDefault()
            :""

            }).ToListAsync();
            ViewBag.Data = result;
            ViewBag.type = type;

        }
        return View();
    }
}


