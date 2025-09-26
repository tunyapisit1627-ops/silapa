using Syncfusion.Pdf;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Syncfusion.Pdf.Graphics;
using Silapa.Models;
using Microsoft.AspNetCore.Identity;
using Syncfusion.Pdf.Parsing;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Drawing;
using Syncfusion.EJ2.BarcodeGenerator;
using Syncfusion.EJ2.Charts;
using System.Text;
using Syncfusion.Pdf.Tables;
using System.Data;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Barcode;
using System.Globalization;
using Syncfusion.EJ2.Linq;
namespace Silapa.Controllers
{

    public class PdfmemberController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");

        public PdfmemberController(ILogger<AdminController> logger, ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _userManager = userManager;
            // _configuration = configuration;
        }
        //[HttpGet]
        public async Task<IActionResult> GenePdfregister(int s_id, int c_id, string dateCheckboxes)
        {
            var dates = dateCheckboxes?.Split(',') ?? Array.Empty<string>();
            System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");
            var datasetting = await _context.setupsystem.Where(s => s.status == "1").FirstOrDefaultAsync();
            var datasql = await _context.school
     .Where(x => x.Id == s_id)
     .Include(x => x.registerheads
         .Where(rh =>
             (!dates.Any() || // กรณี dates ว่าง (ไม่มีเงื่อนไข)
             rh.Competitionlist.racedetails
                 .Any(rd => dates.Contains(rd.daterace))) &&
             (c_id == 0 || rh.Competitionlist.c_id == c_id) &&
             (rh.status == "1" || rh.status == "2") &&
             rh.SettingID == datasetting.id
             )) // กรณี c_id เป็น null หรือมีค่า
     .ThenInclude(rh => rh.Registerdetail) // โหลด Registerdetail
     .FirstOrDefaultAsync();
            int countteam = datasql.registerheads.Count();
            // ตรวจสอบว่า datasql และ registerheads ไม่เป็น null
            int countStudents = datasql?.registerheads?.Sum(rh => rh.Registerdetail.Count(x => x.Type == "student")) ?? 0;
            int countTeachers = datasql?.registerheads?.Sum(rh => rh.Registerdetail.Count(x => x.Type == "teacher")) ?? 0;

            var dataCompetitionlist = await _context.Competitionlist.Where(x => x.status == "1").ToListAsync();
            var dataracedetails = await _context.racedetails.Include(x => x.Racelocation).ToListAsync();
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont bFont = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont ttFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont tFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                PdfSolidBrush brush = new PdfSolidBrush(Color.Black);
                PdfPage page = document.Pages.Add();
                float pageWidth = page.GetClientSize().Width;

                string pathpic = _env.WebRootPath + "/dist/img/AdminLTELogo.png";
                FileStream imageStream = new FileStream(pathpic, FileMode.Open, FileAccess.Read);
                PdfBitmap image1 = new PdfBitmap(imageStream);

                float width = 75;
                float height = 75;
                float x = (pageWidth - width) / 2;
                float y = 0;
                page.Graphics.DrawImage(image1, x, y, width, height);
                string text1 = $"{datasetting.name}";
                string text2 = $"สรุปการลงทะเบียน {datasql.Name}";
                string text3 = $"จำนวนรายการที่ลงทะเบียน {countteam} รายการ จำนวนนักเรียน {countStudents} คน จำนวนครู {countTeachers} คน";
                float xCenter1 = (page.GetClientSize().Width - bFont.MeasureString(text1).Width) / 2;
                float xCenter2 = (page.GetClientSize().Width - bFont.MeasureString(text2).Width) / 2;
                float xCenter3 = (page.GetClientSize().Width - bFont.MeasureString(text3).Width) / 2;

                float yPosition = 80; // เริ่มต้นที่แนวตั้ง
                float lineSpacing = 15; // ระยะห่างระหว่างบรรทัด
                                        // วาดข้อความลงในหน้า

                float maxWidth = pageWidth - 20; // ลดขอบซ้ายและขวา
                string[] lines1 = SplitTextToFitWidth(text1, page.Graphics, bFont, maxWidth);
                // page.Graphics.DrawString(text1, bFont, brush, new PointF(xCenter1, yPosition));
                //yPosition += lineSpacing;
                // ใช้ PdfStringFormat เพื่อกำหนดให้ข้อความจัดกึ่งกลางทั้งแนวนอนและแนวตั้ง
                PdfStringFormat centerAlignment = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Middle,

                };
                foreach (string line in lines1)
                {
                    RectangleF rectLine = new RectangleF(0, yPosition, pageWidth, bFont.Height);
                    page.Graphics.DrawString(line, bFont, PdfBrushes.Black, rectLine, centerAlignment);
                    yPosition += bFont.Height; // ขยับตำแหน่ง Y สำหรับบรรทัดถัดไป
                }
                page.Graphics.DrawString(text2, bFont, brush, new PointF(xCenter2, yPosition));
                yPosition += lineSpacing;
                page.Graphics.DrawString(text3, bFont, brush, new PointF(xCenter3, yPosition));
                yPosition += lineSpacing;
                ///รายการแข่งขัน
                ///
                PdfGrid pdfGrid = new PdfGrid();
                pdfGrid.Columns.Add(4);
                pdfGrid.Columns[0].Width = 150;
                pdfGrid.Columns[1].Width = 150;
                int no = 1;
                foreach (var dr in datasql.registerheads.OrderBy(x => x.c_id))
                {
                    //var data=dataCompetitionlist.Where(x=>x.c_id==dr.c_id).FirstOrDefault();
                    PdfGridRow headerRow = pdfGrid.Rows.Add();


                    // กำหนดข้อความในเซลล์แถวแรก
                    headerRow.Cells[0].Value = $"{no}.{dr.Competitionlist.Name}";
                    // รวมเซลล์ 4 คอลัมน์
                    headerRow.Cells[0].ColumnSpan = 4;

                    // จัดข้อความให้อยู่ตรงกลาง
                    headerRow.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Left, // จัดกึ่งกลางแนวนอน
                        LineAlignment = PdfVerticalAlignment.Middle // จัดกึ่งกลางแนวตั้ง
                    };
                    headerRow.Cells[0].Style.Font = bFont;
                    headerRow.Cells[0].Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส

                    PdfGridRow headerRow1 = pdfGrid.Rows.Add();
                    headerRow1.Cells[0].Value = "นักเรียน";
                    headerRow1.Cells[1].Value = "ครู";
                    headerRow1.Cells[2].Value = "รายละเอียด";
                    headerRow1.Cells[2].ColumnSpan = 2;

                    PdfStringFormat stringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,         // จัดข้อความกึ่งกลางแนวนอน
                        LineAlignment = PdfVerticalAlignment.Middle  // จัดข้อความกึ่งกลางแนวตั้ง
                    };
                    headerRow1.Cells[0].StringFormat = stringFormat;
                    headerRow1.Cells[1].StringFormat = stringFormat;
                    headerRow1.Cells[2].StringFormat = stringFormat;
                    // headerRow1.Cells[3].StringFormat = stringFormat;
                    headerRow1.Cells[0].Style.Font = bFont;
                    headerRow1.Cells[1].Style.Font = bFont;
                    headerRow1.Cells[2].Style.Font = bFont;
                    // headerRow1.Cells[3].Style.Font = bFont;




                    ////ข้อมูลนักเรียนและครู
                    ///
                    string sname = "";
                    int scount = 1;
                    string tname = "";
                    int tcount = 1;
                    string date = "";
                    var datar = dataracedetails.Where(x => x.c_id == dr.c_id)?.FirstOrDefault();
                    string location = datar?.Racelocation?.name ?? "ไม่มีข้อมูล";
                    location += $"\nอาคาร {datar?.building ?? ""}\n ห้อง {datar?.room ?? ""}";
                    foreach (var drr in dr.Registerdetail.Where(x => x.h_id == dr.id && x.Type == "student").OrderBy(x => x.no))
                    {
                        sname += $"{scount}.{drr.Prefix}{drr.FirstName} {drr.LastName}\n";
                        scount += 1;
                    }
                    foreach (var drr in dr.Registerdetail.Where(x => x.h_id == dr.id && x.Type == "teacher").OrderBy(x => x.no))
                    {
                        tname += $"{tcount}.{drr.Prefix}{drr.FirstName} {drr.LastName}\n";
                        tcount += 1;
                    }

                    string competitionDetails = GetCompetitionDetails(dr.c_id, thaiCulture);
                    PdfGridRow headerRow2 = pdfGrid.Rows.Add();
                    headerRow2.Cells[0].Value = sname;
                    headerRow2.Cells[1].Value = tname;
                    headerRow2.Cells[2].Value = competitionDetails;
                    headerRow2.Cells[2].ColumnSpan = 2;

                    PdfStringFormat stringFormat2 = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Left,         // จัดข้อความกึ่งกลางแนวนอน
                        LineAlignment = PdfVerticalAlignment.Top  // จัดข้อความกึ่งกลางแนวตั้ง
                    };
                    headerRow2.Cells[0].StringFormat = stringFormat2;
                    headerRow2.Cells[1].StringFormat = stringFormat2;
                    headerRow2.Cells[2].StringFormat = stringFormat2;
                    // headerRow2.Cells[3].StringFormat = stringFormat2;
                    headerRow2.Cells[0].Style.Font = ttFont16;
                    headerRow2.Cells[1].Style.Font = ttFont16;
                    headerRow2.Cells[2].Style.Font = tFont12;
                    //headerRow2.Cells[3].Style.Font = tFont10;




                    no += 1;
                }
                pdfGrid.Draw(page, new PointF(10, yPosition + 15));
                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", "สรุปการลงทะเบียนแข่งขัน.pdf");
                }
            }
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
        private static string[] SplitTextToFitWidth(string text, PdfGraphics graphics, PdfFont font, float maxWidth)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' '); // แยกข้อความเป็นคำ
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float textWidth = font.MeasureString(testLine).Width;

                if (textWidth <= maxWidth)
                {
                    currentLine = testLine; // ถ้าข้อความยังไม่เกินความกว้าง ให้เพิ่มคำเข้าไป
                }
                else
                {
                    lines.Add(currentLine); // บันทึกข้อความที่เต็มบรรทัดแล้ว
                    currentLine = word; // เริ่มข้อความใหม่
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine); // บันทึกข้อความบรรทัดสุดท้าย
            }

            return lines.ToArray();
        }
        public async Task<IActionResult> GenePdfprintScoreDocument(int h_id)
        {
            var setupsystem = await _context.setupsystem.FirstOrDefaultAsync();


            string name = "";
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfPage page = document.Pages.Add();

                var registrationData = await _context.Registerhead.Where(x => x.c_id == h_id)
                .AsNoTracking()
                .Include(x => x.Competitionlist)
                .Include(x => x.School)
                .ToListAsync();
                PdfGraphics graphics = page.Graphics;
                using (FileStream logoStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                {
                    // สร้าง PdfBitmap จากภาพโลโก้
                    PdfBitmap logoImage = new PdfBitmap(logoStream);

                    // กำหนดตำแหน่งโลโก้ที่ต้องการในหน้า (ตัวอย่างที่มุมซ้ายบน)
                    float logoX = 220;  // ระยะห่างจากซ้าย
                    float logoY = 0;  // ระยะห่างจากด้านบน
                    float logoWidth = 70;  // กำหนดความกว้างของโลโก้
                    float logoHeight = 70;  // กำหนดความสูงของโลโก้

                    // วาดโลโก้บนหน้า PDF
                    graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                }
                graphics.DrawString($"ตรวจสอบคะแนน {registrationData[0].Competitionlist.Name}", bFont16, PdfBrushes.Black, new PointF(0, 70));
                name += registrationData[0].Competitionlist.Name;

                // สร้างกริด PDF
                PdfLightTable table = new PdfLightTable();
                // แปลงข้อมูลเป็น DataTable
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("ลำดับ");
                dataTable.Columns.Add("ชื่อโรงเรียน");
                dataTable.Columns.Add("คะแนน");


                int index = 1;
                foreach (var item in registrationData)
                {
                    dataTable.Rows.Add(index, item.School.Name, item.score);
                    index++;
                }

                // กำหนด DataSource ให้กับ PdfGrid
                table.DataSource = dataTable;
                table.Columns[0].Width = 50;
                table.Columns[1].Width = 350;
                table.Columns[2].Width = 100;
                PdfCellStyle defStyle = new PdfCellStyle();
                defStyle.Font = bFont16;
                defStyle.BackgroundBrush = PdfBrushes.White;

                // defStyle.BorderPen = borderPen;
                defStyle.StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

                table.Style.DefaultStyle = defStyle;

                table.Style.ShowHeader = true;

                //Repeate header in all the pages
                table.Style.RepeatHeader = true;
                //Set header data from column caption
                table.Style.HeaderSource = PdfHeaderSource.ColumnCaptions;
                PdfStringFormat format11 = new PdfStringFormat();
                PdfStringFormat format22 = new PdfStringFormat();

                //Set the text Alignment
                format11.Alignment = PdfTextAlignment.Left;
                format22.Alignment = PdfTextAlignment.Center;

                //Set the line Alignment
                format22.LineAlignment = PdfVerticalAlignment.Top;
                table.Columns[0].StringFormat = format22;
                table.Columns[1].StringFormat = format11;
                table.Columns[2].StringFormat = format22;


                // วาด PdfGrid บนหน้า PDF
                table.Draw(page, new PointF(0, 90));


                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"สรุปการกรอกคะแนน{name}.pdf");
                }
            }
        }
        public async Task<IActionResult> GenePdfprintScoreDocument1(int h_id)
        {
            var setupsystem = await _context.setupsystem.FirstOrDefaultAsync();


            string name = "";
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                PdfPage page = document.Pages.Add();

                var registrationData = await _context.Registerhead.Where(x => x.c_id == h_id)
                .AsNoTracking()
                .Include(x => x.Competitionlist)
                .Include(x => x.School)
                .OrderBy(x => x.rank)
                .ToListAsync();
                PdfGraphics graphics = page.Graphics;
                using (FileStream logoStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                {
                    // สร้าง PdfBitmap จากภาพโลโก้
                    PdfBitmap logoImage = new PdfBitmap(logoStream);

                    // กำหนดตำแหน่งโลโก้ที่ต้องการในหน้า (ตัวอย่างที่มุมซ้ายบน)
                    float logoX = 220;  // ระยะห่างจากซ้าย
                    float logoY = 0;  // ระยะห่างจากด้านบน
                    float logoWidth = 70;  // กำหนดความกว้างของโลโก้
                    float logoHeight = 70;  // กำหนดความสูงของโลโก้

                    // วาดโลโก้บนหน้า PDF
                    graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                }
                graphics.DrawString($"ผลคะแนน {registrationData[0].Competitionlist.Name}", bFont16, PdfBrushes.Black, new PointF(0, 70));
                name += registrationData[0].Competitionlist.Name;

                // สร้างกริด PDF
                PdfLightTable table = new PdfLightTable();
                // แปลงข้อมูลเป็น DataTable
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("ลำดับ");
                dataTable.Columns.Add("ชื่อโรงเรียน");
                dataTable.Columns.Add("คะแนน");
                dataTable.Columns.Add("รางวัล");
                dataTable.Columns.Add("อันดับ");


                int index = 1;
                foreach (var item in registrationData)
                {
                    dataTable.Rows.Add(index, item.School.Name, item.score, item.award, item.rank);
                    index++;
                }

                // กำหนด DataSource ให้กับ PdfGrid
                table.DataSource = dataTable;
                table.Columns[0].Width = 50;
                table.Columns[1].Width = 250;
                table.Columns[2].Width = 50;
                table.Columns[3].Width = 100;
                table.Columns[4].Width = 50;
                PdfCellStyle defStyle = new PdfCellStyle();
                defStyle.Font = bFont16;
                defStyle.BackgroundBrush = PdfBrushes.White;

                // defStyle.BorderPen = borderPen;
                defStyle.StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

                table.Style.DefaultStyle = defStyle;

                table.Style.ShowHeader = true;

                //Repeate header in all the pages
                table.Style.RepeatHeader = true;
                //Set header data from column caption
                table.Style.HeaderSource = PdfHeaderSource.ColumnCaptions;
                PdfStringFormat format11 = new PdfStringFormat();
                PdfStringFormat format22 = new PdfStringFormat();

                //Set the text Alignment
                format11.Alignment = PdfTextAlignment.Left;
                format22.Alignment = PdfTextAlignment.Center;

                //Set the line Alignment
                format22.LineAlignment = PdfVerticalAlignment.Top;
                table.Columns[0].StringFormat = format22;
                table.Columns[1].StringFormat = format11;
                table.Columns[2].StringFormat = format22;
                table.Columns[3].StringFormat = format22;
                table.Columns[4].StringFormat = format22;


                // วาด PdfGrid บนหน้า PDF
                table.Draw(page, new PointF(0, 90));


                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"สรุปการกรอกคะแนน{name}.pdf");
                }
            }
        }
        public async Task<IActionResult> GenePdfresult(int c_id)
        {
            var setupsystem = await _context.setupsystem.FirstOrDefaultAsync();
            var user = await _userManager.GetUserAsync(User);
            List<int> m_idList = user.m_id.Split(',')
                        .Select(int.Parse)
                        .ToList();
            string name = "สรุปผลการแข่งขัน";
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);

                var competitionDetails = _context.Competitionlist
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .AsQueryable();

                if (c_id != 0)
                {
                    competitionDetails = competitionDetails.Where(x => x.c_id == c_id);
                }
                if (m_idList.Any())
                {
                    competitionDetails = competitionDetails.Where(x => x.c_id.HasValue && m_idList.Contains(x.c_id.Value));
                }
                var datacom = await competitionDetails.ToListAsync();
                var query = _context.Registerhead
     .AsNoTracking()
     .Include(x => x.Registerdetail)
     .Include(x => x.Competitionlist)
     .Include(x => x.School)
     .OrderByDescending(x => x.score)
     .AsQueryable();

                query = query.Where(x => x.status != "0");
                // Apply the c_id filter if c_id is not zero
                if (c_id != 0)
                {
                    query = query.Where(x => x.Competitionlist.c_id == c_id);
                }

                // Apply the m_idList filter if m_idList is not empty
                if (m_idList.Any())
                {
                    query = query.Where(x => x.Competitionlist.c_id.HasValue && m_idList.Contains(x.Competitionlist.c_id.Value));
                }

                query = query.Where(x => x.status == "2");

                // Execute the query and retrieve the data
                var registrationData = await query.ToListAsync();
                // สร้างหน้าแรกของ PDF
                PdfPage page1 = document.Pages.Add();
                PdfGraphics graphics1 = page1.Graphics;
                using (FileStream logoStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                {
                    // สร้าง PdfBitmap จากภาพโลโก้
                    PdfBitmap logoImage = new PdfBitmap(logoStream);

                    // กำหนดตำแหน่งโลโก้ที่ต้องการในหน้า (ตัวอย่างที่มุมซ้ายบน)
                    float logoX = 220;  // ระยะห่างจากซ้าย
                    float logoY = 0;  // ระยะห่างจากด้านบน
                    float logoWidth = 70;  // กำหนดความกว้างของโลโก้
                    float logoHeight = 70;  // กำหนดความสูงของโลโก้

                    // วาดโลโก้บนหน้า PDF
                    graphics1.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                }
                int yPosition = 70;
                var category = await _context.category.Where(x => m_idList.Contains(x.Id)).ToListAsync();
                var categoryname = "";
                foreach (var dr in category)
                {
                    categoryname += $" {dr.Name}";
                }
                // เพิ่มโลโก้และหัวเรื่อง
                //  graphics1.DrawImage(logoImage, new PointF(220, 0));
                string text = $"{setupsystem.name} \n {categoryname}";
                // สร้าง PdfTextElement
                PdfTextElement textElement = new PdfTextElement(text, bFont16, PdfBrushes.Black)
                {
                    StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center, // จัดกึ่งกลาง
                    }
                };

                // กำหนดพื้นที่
                RectangleF bounds = new RectangleF(0, yPosition, page1.Size.Width - 60, 0); // 0 หมายถึงสูงอัตโนมัติ

                // วาดข้อความ
                // วาดข้อความแรก
                PdfLayoutResult result = textElement.Draw(page1, bounds);

                // คำนวณตำแหน่ง Y สำหรับบรรทัดถัดไป
                yPosition = (int)(result.Bounds.Bottom + 10); // เพิ่มระยะห่าง 10 หน่วย
                graphics1.DrawString($"สรุปผลการแข่งขัน", bFont16, PdfBrushes.Black, new PointF(0, yPosition));
                var totalTeams = registrationData.Count();
                int studentCount = registrationData
    .SelectMany(rh => rh.Registerdetail)
    .Count(rd => rd.Type == "student");

                // Count the number of teachers
                int teacherCount = registrationData
                    .SelectMany(rh => rh.Registerdetail)
                    .Count(rd => rd.Type == "teacher");

                int goldMedalCount = registrationData
.Count(rh => rh.award == "เหรียญทอง");

                // Count the number of silver medals
                int silverMedalCount = registrationData
                    .Count(rh => rh.award == "เหรียญเงิน");

                // Count the number of bronze medals
                int bronzeMedalCount = registrationData
                    .Count(rh => rh.award == "เหรียญทองแดง");

                // Count the number of bronze medals
                int MedalCount = registrationData
                    .Count(rh => rh.award == "เข้าร่วม");

                // คำนวณเปอร์เซ็นต์ของเหรียญรางวัล
                double goldPercentage = (double)goldMedalCount / totalTeams * 100;
                double silverPercentage = (double)silverMedalCount / totalTeams * 100;
                double bronzePercentage = (double)bronzeMedalCount / totalTeams * 100;
                double Percentage = (double)MedalCount / totalTeams * 100;


                ///จำนวนรายการ
                ///
                int competitionscount = datacom.Count();
                /// ไม่มาแข่งขัน
                /// 
                int notcompetitions = _context.Registerhead
                .Where(x => x.Competitionlist.c_id.HasValue && m_idList.Contains(x.Competitionlist.c_id.Value))
                    .Count(rh => rh.award == "ไม่ได้แข่งขัน");


                // แสดงสถิติ
                graphics1.DrawString($"จำนวนรายการทั้งหมด: {competitionscount} รายการ", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"จำนวนทีมที่เข้าร่วม: {totalTeams} ทีม", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"จำนวนนักเรียนที่เข้าร่วม: {studentCount} คน", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"จำนวนครูที่เข้าร่วม: {teacherCount} คน", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"เหรียญทอง: {goldMedalCount} ({goldPercentage:F2}%)", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"เหรียญเงิน: {silverMedalCount} ({silverPercentage:F2}%)", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"เหรียญทองแดง: {bronzeMedalCount} ({bronzePercentage:F2}%)", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"เข้าร่วม: {MedalCount} ({Percentage:F2}%)", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));
                graphics1.DrawString($"ไม่มาแข่งขัน: {notcompetitions} ทีม", bFont16, PdfBrushes.Black, new PointF(0, yPosition += 15));

                yPosition += 20;
                // สร้างกราฟแสดงผล (ตัวอย่างการสร้างกราฟวงกลม)
                // ข้อมูลที่คำนวณได้
                float[] medalPercentages = {
    (float)goldPercentage,
    (float)silverPercentage,
    (float)bronzePercentage,
    (float)Percentage
};
                string[] medalLabels = { "เหรียญทอง", "เหรียญเงิน", "เหรียญทองแดง", "เข้าร่วม" };

                // สีสำหรับแต่ละส่วนของกราฟ
                PdfBrush[] medalBrushes = {
    PdfBrushes.Gold,
    PdfBrushes.Silver,
    PdfBrushes.Brown,
    PdfBrushes.LightCyan
};

                // ตำแหน่งและขนาดของกราฟวงกลม
                RectangleF pieBounds = new RectangleF(150, yPosition + 20, 200, 200);

                // วาดกราฟวงกลม
                float startAngle = 0;


                for (int i = 0; i < medalPercentages.Length; i++)
                {
                    float sweepAngle = medalPercentages[i] * 360 / 100; // เปลี่ยนเปอร์เซ็นต์เป็นองศา
                    graphics1.DrawPie(PdfPens.Black, medalBrushes[i], pieBounds, startAngle, sweepAngle);

                    // วางตำแหน่งถัดไป
                    startAngle += sweepAngle;
                }

                // เพิ่มคำอธิบาย (Legend)
                float legendX = 400;
                float legendY = yPosition + 30;

                for (int i = 0; i < medalLabels.Length; i++)
                {
                    // วาดสี่เหลี่ยมสี
                    RectangleF colorBox = new RectangleF(legendX, legendY, 10, 10);
                    graphics1.DrawRectangle(PdfPens.Black, medalBrushes[i], colorBox);

                    // เพิ่มข้อความคำอธิบาย
                    graphics1.DrawString(medalLabels[i], bFont12, PdfBrushes.Black, new PointF(legendX + 15, legendY));
                    legendY += 15; // ระยะห่างระหว่างแต่ละรายการ
                }

                foreach (var dr in datacom)
                {
                    PdfPage page = document.Pages.Add();
                    PdfGraphics graphics = page.Graphics;
                    using (FileStream logoStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                    {
                        // สร้าง PdfBitmap จากภาพโลโก้
                        PdfBitmap logoImage = new PdfBitmap(logoStream);

                        // กำหนดตำแหน่งโลโก้ที่ต้องการในหน้า (ตัวอย่างที่มุมซ้ายบน)
                        float logoX = 220;  // ระยะห่างจากซ้าย
                        float logoY = 0;  // ระยะห่างจากด้านบน
                        float logoWidth = 70;  // กำหนดความกว้างของโลโก้
                        float logoHeight = 70;  // กำหนดความสูงของโลโก้

                        // วาดโลโก้บนหน้า PDF
                        graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                    }
                    graphics.DrawString($"ผลการแข่งขัน {dr.Name}", bFont16, PdfBrushes.Black, new PointF(0, 70));


                    // สร้างกริด PDF
                    PdfLightTable table = new PdfLightTable();
                    // แปลงข้อมูลเป็น DataTable
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("ลำดับ");
                    dataTable.Columns.Add("ชื่อโรงเรียน");
                    dataTable.Columns.Add("คะแนน");
                    dataTable.Columns.Add("รางวัล");
                    dataTable.Columns.Add("อันดับ");



                    int index = 1;
                    var data = registrationData.Where(x => x.Competitionlist.Id == dr.Id).ToList();
                    foreach (var item in data)
                    {
                        // กำหนดคำอธิบายอันดับตามค่า item.rank
                        string rankDescription;
                        switch (item.rank)
                        {
                            case 1:
                                rankDescription = "ชนะเลิศ";
                                break;
                            case 2:
                                rankDescription = "รองชนะเลิศ อันดับ 1";
                                break;
                            case 3:
                                rankDescription = "รองชนะเลิศ อันดับ 2";
                                break;
                            default:
                                rankDescription = item.rank.ToString();
                                break;
                        }
                        dataTable.Rows.Add(index, item.School.Name, item.score, item.award, rankDescription);
                        index++;
                    }

                    // กำหนด DataSource ให้กับ PdfGrid
                    table.DataSource = dataTable;
                    table.Columns[0].Width = 50;
                    table.Columns[1].Width = 200;
                    table.Columns[2].Width = 50;
                    table.Columns[3].Width = 100;
                    table.Columns[4].Width = 100;
                    PdfCellStyle defStyle = new PdfCellStyle();
                    defStyle.Font = bFont16;
                    defStyle.BackgroundBrush = PdfBrushes.White;

                    // defStyle.BorderPen = borderPen;
                    defStyle.StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

                    table.Style.DefaultStyle = defStyle;

                    table.Style.ShowHeader = true;

                    //Repeate header in all the pages
                    table.Style.RepeatHeader = true;
                    //Set header data from column caption
                    table.Style.HeaderSource = PdfHeaderSource.ColumnCaptions;
                    PdfStringFormat format11 = new PdfStringFormat();
                    PdfStringFormat format22 = new PdfStringFormat();

                    //Set the text Alignment
                    format11.Alignment = PdfTextAlignment.Left;
                    format22.Alignment = PdfTextAlignment.Center;

                    //Set the line Alignment
                    format22.LineAlignment = PdfVerticalAlignment.Top;
                    table.Columns[0].StringFormat = format22;
                    table.Columns[1].StringFormat = format11;
                    table.Columns[2].StringFormat = format22;
                    table.Columns[3].StringFormat = format22;
                    table.Columns[4].StringFormat = format22;


                    // วาด PdfGrid บนหน้า PDF
                    table.Draw(page, new PointF(0, 90));
                }

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"สรุปผลการแข่งขัน.pdf");
                }
            }
        }
        // ฟังก์ชันสำหรับแปลงตัวเลขอารบิกเป็นเลขไทย
        string ConvertToThaiDigits(string input)
        {
            char[] arabicDigits = "0123456789".ToCharArray();
            char[] thaiDigits = "๐๑๒๓๔๕๖๗๘๙".ToCharArray();

            foreach (var digit in arabicDigits)
            {
                input = input.Replace(digit, thaiDigits[digit - '0']);
            }

            return input;
        }
        public async Task<IActionResult> GenePdfCertificate(int settingId, int h_id, string type)
        {
            var setupsystem = await _context.setupsystem.Where(x => x.id == settingId).FirstOrDefaultAsync();
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont28 = new PdfTrueTypeFont(fontStream, 28, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont18 = new PdfTrueTypeFont(fontStream, 18, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                // Configure page settings for an A4 landscape page
                PdfPageSettings pageSettings = new PdfPageSettings
                {
                    Size = PdfPageSize.A4,
                    Orientation = PdfPageOrientation.Landscape
                };
                #region 
                if (type == "1")
                {

                    /*  var participantDetails = await _context.Registerhead
      .Where(x => x.id == h_id) // ค้นหา Registerhead ด้วย h_id
      .Include(x => x.Registerdetail) // รวมข้อมูลนักเรียนและครู
      .Include(x => x.School)
      .Include(x => x.Competitionlist) // รวมข้อมูลรายการแข่งขัน
          .ThenInclude(x => x.racedetails) // รวม RaceDetails
          .ThenInclude(x => x.Racelocation) // รวม Racelocation
      .SelectMany(r => r.Registerdetail.Select(rd => new
      {
          No_id = rd.no,
          FullName = rd.Prefix + rd.FirstName + " " + rd.LastName, // ชื่อเต็มของนักเรียน/ครู
          RoleDescription = rd.Type == "teacher"
              ? "เป็นครูผู้ฝึกสอนนักเรียน"
                  : "", // ค่าเริ่มต้นหากไม่ใช่ teacher หรือ student
          School = r.School.Name,
          Award = r.award, // รางวัล
          Rank = r.rank == 1 ? "ชนะเลิศ" :
                 r.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                 r.rank == 3 ? "รองชนะเลิศ อันดับ ๒" :
                 "", // อันดับ
          CompetitionName = r.Competitionlist.Name, // ชื่อรายการแข่งขัน
          Type1 = rd.Type,
          Location = r.Competitionlist.racedetails.FirstOrDefault().Racelocation.name // สถานที่แข่งขัน
      }))
      .OrderBy(x => x.RoleDescription)
      .ToListAsync();*/
                    // ดึงข้อมูล Registerhead พร้อมข้อมูลที่เกี่ยวข้อง
                    var registerHead = await _context.Registerhead
                        .Where(x => x.id == h_id)
                        .Include(x => x.School)
                        .Include(x => x.Competitionlist)
                            .ThenInclude(x => x.racedetails)
                            .ThenInclude(x => x.Racelocation)
                        .FirstOrDefaultAsync();

                    if (registerHead == null)
                    {
                        throw new Exception("ไม่พบข้อมูล Registerhead");
                    }

                    // ดึงข้อมูล Registerdetail ที่เชื่อมโยงกับ Registerhead
                    var registerDetails = await _context.Registerdetail
                        .Where(rd => rd.h_id == h_id)
                        .ToListAsync();

                    // รวมข้อมูลใน C#
                    var raceDetail = registerHead.Competitionlist?.racedetails.FirstOrDefault();
                    var raceLocationName = raceDetail?.Racelocation?.name;

                    var participantDetails = registerDetails
                        .Select(rd => new
                        {
                            No_id = rd.no,
                            FullName = $"{rd.Prefix}{rd.FirstName} {rd.LastName}",
                            RoleDescription = rd.Type == "teacher" ? "เป็นครูผู้ฝึกสอนนักเรียน" : "",
                            School = registerHead.School?.Name,
                            Award = registerHead.award,
                            Rank = registerHead.rank == 1 ? "ชนะเลิศ" :
                                   registerHead.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                                   registerHead.rank == 3 ? "รองชนะเลิศ อันดับ ๒" : "",
                            CompetitionName = registerHead.Competitionlist?.Name,
                            Type1 = rd.Type,
                            Location = raceLocationName
                        })
                        .OrderBy(x => x.RoleDescription)
                        .ToList();

                    foreach (var dr in participantDetails)
                    {
                        // Add a section with the specified settings
                        PdfSection section = document.Sections.Add();
                        section.PageSettings = pageSettings;

                        // Add a page to the section
                        PdfPage page = section.Pages.Add();

                        // Get the graphics object to draw on the page
                        PdfGraphics graphics = page.Graphics;
                        // Load the background image
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "card", $"{setupsystem.certificate}");
                        using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            PdfBitmap backgroundImage = new PdfBitmap(imageStream);

                            // Draw the background image to cover the entire page
                            graphics.DrawImage(backgroundImage, 0, 0, page.Graphics.ClientSize.Width, page.Graphics.ClientSize.Height);
                        }
                        PdfBrush brush = PdfBrushes.Black;
                        PdfBrush brush1 = PdfBrushes.DarkBlue;
                        int yPostion = 230;

                        // Example: Add recipient's name
                        string recipientName = $"{dr.FullName}"; // Replace with dynamic data
                        SizeF textSize = BFont28.MeasureString(recipientName);
                        float xPosition1 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        recipientName,
                                        BFont28,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition1 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            recipientName,
                            BFont28,
                            brush1,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion) // Centered horizontally
                        );
                        yPostion += 30;
                        // Example: Add award title
                        string schoolTitle = $"{dr.School}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(schoolTitle);
                        float xPosition2 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        schoolTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition2 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            schoolTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        yPostion += 20;

                        // Example: Add award title
                        string awardTitle = $"{dr.RoleDescription} ได้รับรางวัล {dr.Rank} {dr.Award}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(awardTitle);
                        float xPosition3 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        awardTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition3 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            awardTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        yPostion += 20;
                        // Example: Add award title
                        string CompetitionTitle = $"{ConvertToThaiDigits(dr.CompetitionName)}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(CompetitionTitle);
                        float xPosition4 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        CompetitionTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition4 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            CompetitionTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        //
                        yPostion += 20;
                        // Example: Add award title
                        string locationTitle = $"ณ {dr.Location}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(locationTitle);
                        float xPosition5 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        locationTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition5 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            locationTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        ///เลขที่
                        // ข้อความของเลขที่เกียรติบัตร
                        var datacertificate = await _context.Certificate.Where(x => x.RegistrationID == h_id && x.RegistrationNo == dr.No_id && x.type == dr.Type1).FirstOrDefaultAsync();
                        string certificateNumber = ""; // แทนด้วยข้อมูลจริง
                        string gencode = "";
                        if (datacertificate != null)
                        {
                            // ถ้ามีข้อมูลแล้ว ดึง CertificateNumber มาใช้
                            certificateNumber = $"เลขที่เกียรติบัตร: {ConvertToThaiDigits(datacertificate.CertificateNumber)}";
                            gencode = datacertificate.CertificateNumber;
                        }
                        else
                        {
                            // ถ้ายังไม่มีข้อมูล ให้สร้างเลขที่เกียรติบัตรใหม่
                            string lastCertificateNumber = await _context.Certificate
                                .Where(x => x.SettingID == setupsystem.id)
                                .OrderByDescending(c => c.CertificateID)
                                .Select(c => c.CertificateNumber)
                                .FirstOrDefaultAsync();

                            // สร้างเลขที่เกียรติบัตรใหม่
                            if (string.IsNullOrEmpty(lastCertificateNumber))
                            {
                                lastCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/0000001"; // เริ่มต้นเลขใหม่
                                gencode = $"{setupsystem.time}.{setupsystem.yaer}/0000001";
                            }

                            string[] parts = lastCertificateNumber.Split('/');
                            int lastNumber = int.Parse(parts[1]);
                            int newNumber = lastNumber + 1;

                            string newCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
                            gencode = newCertificateNumber;
                            // บันทึกข้อมูลใหม่ลงฐานข้อมูล
                            var newCertificate = new Certificate
                            {
                                RegistrationID = h_id,
                                RegistrationNo = dr.No_id,
                                SettingID = setupsystem.id,
                                AwardID = dr.Rank,
                                AwardName = dr.Award,
                                type = dr.Type1,
                                Description = dr.CompetitionName,
                                CertificateNumber = newCertificateNumber,
                                IssueDate = DateTime.Now,
                                lastupdate = DateTime.Now
                            };

                            _context.Certificate.Add(newCertificate);
                            await _context.SaveChangesAsync();

                            // แปลงเลขใน lastCertificateNumber
                            string thaiCertificateNumber = ConvertToThaiDigits(newCertificateNumber);
                            // ใช้เลขที่เกียรติบัตรใหม่
                            certificateNumber = $"เลขที่เกียรติบัตร: {thaiCertificateNumber}";
                        }

                        // คำนวณตำแหน่งสำหรับวางข้อความชิดขวา
                        SizeF textSizecertificateNumber = BFont16.MeasureString(certificateNumber); // วัดขนาดข้อความ
                        float xPosition6 = 20; // ชิดขวาโดยเว้นระยะขอบ 10 หน่วย
                        float yPosition6 = 10; // ระยะจากด้านบน


                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        certificateNumber,
                                        BFont16,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition6 + dx, yPosition6 + dy)
                                    );
                                }
                            }
                        }
                        // วาดข้อความบนหน้า PDF
                        graphics.DrawString(
                            certificateNumber, // ข้อความ
                            BFont16,              // ฟอนต์
                            brush,             // แปรงสีสำหรับข้อความ
                            new PointF(xPosition6, yPosition6) // ตำแหน่งมุมบนขวา
                        );
                        ///barcode
                        ///
                        PdfQRBarcode qrbarcodelogo = new PdfQRBarcode();
                        qrbarcodelogo.InputMode = InputMode.BinaryMode;
                        // Automatically select the Version
                        // qrbarcodelogo.Version = QRCodeVersion.Auto;
                        // Set the Error correction level to high
                        qrbarcodelogo.ErrorCorrectionLevel = PdfErrorCorrectionLevel.High;
                        // Set dimension for each block
                        qrbarcodelogo.XDimension = 2;
                        qrbarcodelogo.Text = "https://korat.sillapas.com/Home/frmexamine?gencode=" + gencode;
                        //Set the logo image to QR barcode. 

                        using (FileStream imageStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                        {
                            Syncfusion.Pdf.Barcode.QRCodeLogo qRCodeLogo = new Syncfusion.Pdf.Barcode.QRCodeLogo(imageStream);
                            qrbarcodelogo.Logo = qRCodeLogo;
                            // Draw the QR barcode
                            qrbarcodelogo.Draw(page, new PointF(21, 465));
                        }
                    }
                }
                #endregion

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"เกียรติบัตร.pdf");
                }
            }
        }
        public async Task<IActionResult> GenePdfCertificate1(int settingId, int h_id, string type)
        {
            var setupsystem = await _context.setupsystem.Where(x => x.id == settingId).FirstOrDefaultAsync();
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont28 = new PdfTrueTypeFont(fontStream, 28, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont18 = new PdfTrueTypeFont(fontStream, 18, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                // Configure page settings for an A4 landscape page
                PdfPageSettings pageSettings = new PdfPageSettings
                {
                    Size = PdfPageSize.A4,
                    Orientation = PdfPageOrientation.Landscape
                };
                #region 
                if (type == "1")
                {


                    var registerHead = await _context.Registerhead
                        .Where(x => x.id == h_id)
                        .Include(x => x.School)
                        .Include(x => x.Competitionlist)
                            .ThenInclude(x => x.racedetails)
                            .ThenInclude(x => x.Racelocation)
                        .FirstOrDefaultAsync();

                    if (registerHead == null)
                    {
                        throw new Exception("ไม่พบข้อมูล Registerhead");
                    }

                    // ดึงข้อมูล Registerdetail ที่เชื่อมโยงกับ Registerhead
                    var registerDetails = await _context.Registerdetail
                        .Where(rd => rd.h_id == h_id)
                        .ToListAsync();

                    // รวมข้อมูลใน C#
                    var raceDetail = registerHead.Competitionlist?.racedetails.FirstOrDefault();
                    var raceLocationName = raceDetail?.Racelocation?.name;

                    var participantDetails = registerDetails
                        .Select(rd => new
                        {
                            No_id = rd.no,
                            FullName = $"{rd.Prefix}{rd.FirstName} {rd.LastName}",
                            RoleDescription = rd.Type == "teacher" ? "เป็นครูผู้ฝึกสอนนักเรียน" : "",
                            School = registerHead.School?.Name,
                            Award = registerHead.award,
                            Rank = registerHead.rank == 1 ? "ชนะเลิศ" :
                                   registerHead.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                                   registerHead.rank == 3 ? "รองชนะเลิศ อันดับ ๒" : "",
                            CompetitionName = registerHead.Competitionlist?.Name,
                            Type1 = rd.Type,
                            Location = raceLocationName
                        })
                        .OrderBy(x => x.RoleDescription)
                        .ToList();
                    // var hd = await _context.Registerdetail.Where(x => x.h_id == h_id).ToListAsync();

                    foreach (var dr in registerDetails)
                    {
                        var FullName = $"{dr.Prefix}{dr.FirstName} {dr.LastName}";
                        var School = registerHead.School?.Name;
                        var RoleDescription = dr.Type == "teacher" ? "เป็นครูผู้ฝึกสอนนักเรียน" : "";
                        var Rank = registerHead.rank == 1 ? "ชนะเลิศ" :
                                   registerHead.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                                   registerHead.rank == 3 ? "รองชนะเลิศ อันดับ ๒" : "";
                        var Award = registerHead.award;
                        var CompetitionName = registerHead.Competitionlist?.Name;
                        var Location = raceLocationName;
                        var Type1 = dr.Type;
                        // Add a section with the specified settings
                        PdfSection section = document.Sections.Add();
                        section.PageSettings = pageSettings;

                        // Add a page to the section
                        PdfPage page = section.Pages.Add();

                        // Get the graphics object to draw on the page
                        PdfGraphics graphics = page.Graphics;
                        // Load the background image
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "card", $"{setupsystem.certificate}");
                        using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            PdfBitmap backgroundImage = new PdfBitmap(imageStream);

                            // Draw the background image to cover the entire page
                            graphics.DrawImage(backgroundImage, 0, 0, page.Graphics.ClientSize.Width, page.Graphics.ClientSize.Height);
                        }
                        PdfBrush brush = PdfBrushes.Black;
                        PdfBrush brush1 = PdfBrushes.DarkBlue;
                        int yPostion = 230;

                        // Example: Add recipient's name
                        string recipientName = $"{FullName}"; // Replace with dynamic data
                        SizeF textSize = BFont28.MeasureString(recipientName);
                        float xPosition1 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        recipientName,
                                        BFont28,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition1 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            recipientName,
                            BFont28,
                            brush1,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion) // Centered horizontally
                        );
                        yPostion += 30;
                        // Example: Add award title
                        string schoolTitle = $"{School}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(schoolTitle);
                        float xPosition2 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        schoolTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition2 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            schoolTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        yPostion += 20;

                        // Example: Add award title
                        string awardTitle = $"{RoleDescription} ได้รับรางวัล {Rank} {Award}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(awardTitle);
                        float xPosition3 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        awardTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition3 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            awardTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        yPostion += 20;
                        // Example: Add award title
                        string CompetitionTitle = $"{ConvertToThaiDigits(CompetitionName)}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(CompetitionTitle);
                        float xPosition4 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        CompetitionTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition4 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            CompetitionTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        //
                        yPostion += 20;
                        // Example: Add award title
                        string locationTitle = $"ณ {Location}"; // Replace with dynamic data
                        textSize = BFont18.MeasureString(locationTitle);
                        float xPosition5 = (page.GetClientSize().Width - textSize.Width) / 2;
                        // Draw the outline first (white border)
                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        locationTitle,
                                        BFont18,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition5 + dx, yPostion + dy)
                                    );
                                }
                            }
                        }
                        graphics.DrawString(
                            locationTitle,
                            BFont18,
                            brush,
                            new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                        );
                        ///เลขที่
                        // ข้อความของเลขที่เกียรติบัตร
                        var datacertificate = await _context.Certificate.Where(x => x.RegistrationID == h_id && x.RegistrationNo == dr.no && x.type == Type1).FirstOrDefaultAsync();
                        string certificateNumber = ""; // แทนด้วยข้อมูลจริง
                        string gencode = "";
                        if (datacertificate != null)
                        {
                            // ถ้ามีข้อมูลแล้ว ดึง CertificateNumber มาใช้
                            certificateNumber = $"เลขที่เกียรติบัตร: {ConvertToThaiDigits(datacertificate.CertificateNumber)}";
                            gencode = datacertificate.CertificateNumber;
                        }
                        else
                        {
                            // ถ้ายังไม่มีข้อมูล ให้สร้างเลขที่เกียรติบัตรใหม่
                            string lastCertificateNumber = await _context.Certificate
                                .Where(x => x.SettingID == setupsystem.id)
                                .OrderByDescending(c => c.CertificateID)
                                .Select(c => c.CertificateNumber)
                                .FirstOrDefaultAsync();

                            // สร้างเลขที่เกียรติบัตรใหม่
                            if (string.IsNullOrEmpty(lastCertificateNumber))
                            {
                                lastCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/0000001"; // เริ่มต้นเลขใหม่
                                gencode = $"{setupsystem.time}.{setupsystem.yaer}/0000001";
                            }

                            string[] parts = lastCertificateNumber.Split('/');
                            int lastNumber = int.Parse(parts[1]);
                            int newNumber = lastNumber + 1;

                            string newCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
                            gencode = newCertificateNumber;
                            // บันทึกข้อมูลใหม่ลงฐานข้อมูล
                            var newCertificate = new Certificate
                            {
                                RegistrationID = h_id,
                                RegistrationNo = dr.no,
                                SettingID = setupsystem.id,
                                AwardID = Rank,
                                AwardName = Award,
                                type = Type1,
                                Description = CompetitionName,
                                CertificateNumber = newCertificateNumber,
                                IssueDate = DateTime.Now,
                                lastupdate = DateTime.Now
                            };

                            _context.Certificate.Add(newCertificate);
                            await _context.SaveChangesAsync();

                            // แปลงเลขใน lastCertificateNumber
                            string thaiCertificateNumber = ConvertToThaiDigits(newCertificateNumber);
                            // ใช้เลขที่เกียรติบัตรใหม่
                            certificateNumber = $"เลขที่เกียรติบัตร: {thaiCertificateNumber}";
                        }

                        // คำนวณตำแหน่งสำหรับวางข้อความชิดขวา
                        SizeF textSizecertificateNumber = BFont16.MeasureString(certificateNumber); // วัดขนาดข้อความ
                        float xPosition6 = 20; // ชิดขวาโดยเว้นระยะขอบ 10 หน่วย
                        float yPosition6 = 10; // ระยะจากด้านบน


                        for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                        {
                            for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                            {
                                if (dx != 0 || dy != 0) // Skip the center point
                                {
                                    graphics.DrawString(
                                        certificateNumber,
                                        BFont16,
                                        new PdfSolidBrush(Color.White), // White outline
                                        new PointF(xPosition6 + dx, yPosition6 + dy)
                                    );
                                }
                            }
                        }
                        // วาดข้อความบนหน้า PDF
                        graphics.DrawString(
                            certificateNumber, // ข้อความ
                            BFont16,              // ฟอนต์
                            brush,             // แปรงสีสำหรับข้อความ
                            new PointF(xPosition6, yPosition6) // ตำแหน่งมุมบนขวา
                        );
                        ///barcode
                        ///
                        PdfQRBarcode qrbarcodelogo = new PdfQRBarcode();
                        qrbarcodelogo.InputMode = InputMode.BinaryMode;
                        // Automatically select the Version
                        // qrbarcodelogo.Version = QRCodeVersion.Auto;
                        // Set the Error correction level to high
                        qrbarcodelogo.ErrorCorrectionLevel = PdfErrorCorrectionLevel.High;
                        // Set dimension for each block
                        qrbarcodelogo.XDimension = 2;
                        qrbarcodelogo.Text = "https://korat.sillapas.com/Home/frmexamine?gencode=" + gencode;
                        //Set the logo image to QR barcode. 

                        using (FileStream imageStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                        {
                            Syncfusion.Pdf.Barcode.QRCodeLogo qRCodeLogo = new Syncfusion.Pdf.Barcode.QRCodeLogo(imageStream);
                            qrbarcodelogo.Logo = qRCodeLogo;
                            // Draw the QR barcode
                            qrbarcodelogo.Draw(page, new PointF(21, 465));
                        }
                    }
                }
                #endregion

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"เกียรติบัตร.pdf");
                }
            }
        }
        public async Task<IActionResult> GenePdfCertificate2(int settingId, int c_id, int s_id, string type)
        {
            var setupsystem = await _context.setupsystem.Where(x => x.id == settingId).FirstOrDefaultAsync();
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont28 = new PdfTrueTypeFont(fontStream, 28, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont18 = new PdfTrueTypeFont(fontStream, 18, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                // Configure page settings for an A4 landscape page
                PdfPageSettings pageSettings = new PdfPageSettings
                {
                    Size = PdfPageSize.A4,
                    Orientation = PdfPageOrientation.Landscape
                };
                #region 
                if (type == "1")
                {


                    var sqlregisterHead = _context.Registerhead
                        .Where(x => x.s_id == s_id)
                        .Include(x => x.School)
                        .Include(x => x.Competitionlist)
                            .ThenInclude(x => x.racedetails)
                            .ThenInclude(x => x.Racelocation)
                            .AsQueryable();
                    if (c_id != 0)
                    {
                        sqlregisterHead = sqlregisterHead.Where(x => x.Competitionlist.c_id == c_id);
                    }
                    var registerHead = await sqlregisterHead.OrderBy(x => x.c_id).ToListAsync();

                    if (registerHead == null)
                    {
                        throw new Exception("ไม่พบข้อมูล Registerhead");
                    }

                    foreach (var drr in registerHead)
                    {
                        // ดึงข้อมูล Registerdetail ที่เชื่อมโยงกับ Registerhead
                        var registerDetails = await _context.Registerdetail
                            .Where(rd => rd.h_id == drr.id)
                            .ToListAsync();

                        // รวมข้อมูลใน C#
                        var raceDetail = drr.Competitionlist?.racedetails.FirstOrDefault();
                        var raceLocationName = raceDetail?.Racelocation?.name;

                        var participantDetails = registerDetails
                            .Select(rd => new
                            {
                                No_id = rd.no,
                                FullName = $"{rd.Prefix}{rd.FirstName} {rd.LastName}",
                                RoleDescription = rd.Type == "teacher" ? "เป็นครูผู้ฝึกสอนนักเรียน" : "",
                                School = drr.School?.Name,
                                Award = drr.award,
                                Rank = drr.rank == 1 ? "ชนะเลิศ" :
                                       drr.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                                       drr.rank == 3 ? "รองชนะเลิศ อันดับ ๒" : "",
                                CompetitionName = drr.Competitionlist?.Name,
                                Type1 = rd.Type,
                                Location = raceLocationName
                            })
                            .OrderBy(x => x.RoleDescription)
                            .ToList();
                        // var hd = await _context.Registerdetail.Where(x => x.h_id == h_id).ToListAsync();

                        foreach (var dr in registerDetails)
                        {
                            var FullName = $"{dr.Prefix}{dr.FirstName} {dr.LastName}";
                            var School = drr.School?.Name;
                            var RoleDescription = dr.Type == "teacher" ? "เป็นครูผู้ฝึกสอนนักเรียน" : "";
                            var Rank = drr.rank == 1 ? "ชนะเลิศ" :
                                       drr.rank == 2 ? "รองชนะเลิศ อันดับ ๑" :
                                       drr.rank == 3 ? "รองชนะเลิศ อันดับ ๒" : "";
                            var Award = drr.award;
                            var CompetitionName = drr.Competitionlist?.Name;
                            var Location = raceLocationName;
                            var Type1 = dr.Type;
                            // Add a section with the specified settings
                            PdfSection section = document.Sections.Add();
                            section.PageSettings = pageSettings;

                            // Add a page to the section
                            PdfPage page = section.Pages.Add();

                            // Get the graphics object to draw on the page
                            PdfGraphics graphics = page.Graphics;
                            // Load the background image
                            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "card", $"{setupsystem.certificate}");
                            using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                            {
                                PdfBitmap backgroundImage = new PdfBitmap(imageStream);

                                // Draw the background image to cover the entire page
                                graphics.DrawImage(backgroundImage, 0, 0, page.Graphics.ClientSize.Width, page.Graphics.ClientSize.Height);
                            }
                            PdfBrush brush = PdfBrushes.Black;
                            PdfBrush brush1 = PdfBrushes.DarkBlue;
                            int yPostion = 230;

                            // Example: Add recipient's name
                            string recipientName = $"{FullName}"; // Replace with dynamic data
                            SizeF textSize = BFont28.MeasureString(recipientName);
                            float xPosition1 = (page.GetClientSize().Width - textSize.Width) / 2;
                            // Draw the outline first (white border)
                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            recipientName,
                                            BFont28,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition1 + dx, yPostion + dy)
                                        );
                                    }
                                }
                            }
                            graphics.DrawString(
                                recipientName,
                                BFont28,
                                brush1,
                                new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion) // Centered horizontally
                            );
                            yPostion += 30;
                            // Example: Add award title
                            string schoolTitle = $"{School}"; // Replace with dynamic data
                            textSize = BFont18.MeasureString(schoolTitle);
                            float xPosition2 = (page.GetClientSize().Width - textSize.Width) / 2;
                            // Draw the outline first (white border)
                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            schoolTitle,
                                            BFont18,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition2 + dx, yPostion + dy)
                                        );
                                    }
                                }
                            }
                            graphics.DrawString(
                                schoolTitle,
                                BFont18,
                                brush,
                                new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                            );
                            yPostion += 20;

                            // Example: Add award title
                            string awardTitle = $"{RoleDescription} ได้รับรางวัล {Rank} {Award}"; // Replace with dynamic data
                            textSize = BFont18.MeasureString(awardTitle);
                            float xPosition3 = (page.GetClientSize().Width - textSize.Width) / 2;
                            // Draw the outline first (white border)
                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            awardTitle,
                                            BFont18,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition3 + dx, yPostion + dy)
                                        );
                                    }
                                }
                            }
                            graphics.DrawString(
                                awardTitle,
                                BFont18,
                                brush,
                                new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                            );
                            yPostion += 20;
                            // Example: Add award title
                            string CompetitionTitle = $"{ConvertToThaiDigits(CompetitionName)}"; // Replace with dynamic data
                            textSize = BFont18.MeasureString(CompetitionTitle);
                            float xPosition4 = (page.GetClientSize().Width - textSize.Width) / 2;
                            // Draw the outline first (white border)
                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            CompetitionTitle,
                                            BFont18,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition4 + dx, yPostion + dy)
                                        );
                                    }
                                }
                            }
                            graphics.DrawString(
                                CompetitionTitle,
                                BFont18,
                                brush,
                                new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                            );
                            //
                            yPostion += 20;
                            // Example: Add award title
                            string locationTitle = $"ณ {Location}"; // Replace with dynamic data
                            textSize = BFont18.MeasureString(locationTitle);
                            float xPosition5 = (page.GetClientSize().Width - textSize.Width) / 2;
                            // Draw the outline first (white border)
                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            locationTitle,
                                            BFont18,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition5 + dx, yPostion + dy)
                                        );
                                    }
                                }
                            }
                            graphics.DrawString(
                                locationTitle,
                                BFont18,
                                brush,
                                new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                            );
                            ///เลขที่
                            // ข้อความของเลขที่เกียรติบัตร
                            var datacertificate = await _context.Certificate.Where(x => x.RegistrationID == drr.id && x.RegistrationNo == dr.no && x.type == Type1).FirstOrDefaultAsync();
                            string certificateNumber = ""; // แทนด้วยข้อมูลจริง
                            string gencode = "";
                            if (datacertificate != null)
                            {
                                // ถ้ามีข้อมูลแล้ว ดึง CertificateNumber มาใช้
                                certificateNumber = $"เลขที่เกียรติบัตร: {ConvertToThaiDigits(datacertificate.CertificateNumber)}";
                                gencode = datacertificate.CertificateNumber;
                            }
                            else
                            {
                                // ถ้ายังไม่มีข้อมูล ให้สร้างเลขที่เกียรติบัตรใหม่
                                string lastCertificateNumber = await _context.Certificate
                                    .Where(x => x.SettingID == setupsystem.id)
                                    .OrderByDescending(c => c.CertificateID)
                                    .Select(c => c.CertificateNumber)
                                    .FirstOrDefaultAsync();

                                // สร้างเลขที่เกียรติบัตรใหม่
                                if (string.IsNullOrEmpty(lastCertificateNumber))
                                {
                                    lastCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/0000001"; // เริ่มต้นเลขใหม่
                                    gencode = $"{setupsystem.time}.{setupsystem.yaer}/0000001";
                                }

                                string[] parts = lastCertificateNumber.Split('/');
                                int lastNumber = int.Parse(parts[1]);
                                int newNumber = lastNumber + 1;

                                string newCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
                                gencode = newCertificateNumber;
                                // บันทึกข้อมูลใหม่ลงฐานข้อมูล
                                var newCertificate = new Certificate
                                {
                                    RegistrationID = drr.id,
                                    RegistrationNo = dr.no,
                                    SettingID = setupsystem.id,
                                    AwardID = Rank,
                                    AwardName = Award,
                                    type = Type1,
                                    Description = CompetitionName,
                                    CertificateNumber = newCertificateNumber,
                                    IssueDate = DateTime.Now,
                                    lastupdate = DateTime.Now
                                };

                                _context.Certificate.Add(newCertificate);
                                await _context.SaveChangesAsync();

                                // แปลงเลขใน lastCertificateNumber
                                string thaiCertificateNumber = ConvertToThaiDigits(newCertificateNumber);
                                // ใช้เลขที่เกียรติบัตรใหม่
                                certificateNumber = $"เลขที่เกียรติบัตร: {thaiCertificateNumber}";
                            }

                            // คำนวณตำแหน่งสำหรับวางข้อความชิดขวา
                            SizeF textSizecertificateNumber = BFont16.MeasureString(certificateNumber); // วัดขนาดข้อความ
                            float xPosition6 = 20; // ชิดขวาโดยเว้นระยะขอบ 10 หน่วย
                            float yPosition6 = 10; // ระยะจากด้านบน


                            for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                            {
                                for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                                {
                                    if (dx != 0 || dy != 0) // Skip the center point
                                    {
                                        graphics.DrawString(
                                            certificateNumber,
                                            BFont16,
                                            new PdfSolidBrush(Color.White), // White outline
                                            new PointF(xPosition6 + dx, yPosition6 + dy)
                                        );
                                    }
                                }
                            }
                            // วาดข้อความบนหน้า PDF
                            graphics.DrawString(
                                certificateNumber, // ข้อความ
                                BFont16,              // ฟอนต์
                                brush,             // แปรงสีสำหรับข้อความ
                                new PointF(xPosition6, yPosition6) // ตำแหน่งมุมบนขวา
                            );
                            ///barcode
                            ///
                            PdfQRBarcode qrbarcodelogo = new PdfQRBarcode();
                            qrbarcodelogo.InputMode = InputMode.BinaryMode;
                            // Automatically select the Version
                            // qrbarcodelogo.Version = QRCodeVersion.Auto;
                            // Set the Error correction level to high
                            qrbarcodelogo.ErrorCorrectionLevel = PdfErrorCorrectionLevel.High;
                            // Set dimension for each block
                            qrbarcodelogo.XDimension = 2;
                            qrbarcodelogo.Text = "https://korat.sillapas.com/Home/frmexamine?gencode=" + gencode;
                            //Set the logo image to QR barcode. 

                            using (FileStream imageStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                            {
                                Syncfusion.Pdf.Barcode.QRCodeLogo qRCodeLogo = new Syncfusion.Pdf.Barcode.QRCodeLogo(imageStream);
                                qrbarcodelogo.Logo = qRCodeLogo;
                                // Draw the QR barcode
                                qrbarcodelogo.Draw(page, new PointF(21, 465));
                            }
                        }
                    }
                }
                #endregion

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"เกียรติบัตร.pdf");
                }
            }
        }
        public async Task<IActionResult> GenePdfCertificateIndividual(int settingId, int id, int idNo, string name, string SchoolName, string award, string RoleDescription, string Location, string Category, string type)
        {
            var setupsystem = await _context.setupsystem.Where(x => x.id == settingId).FirstOrDefaultAsync();
            using (PdfDocument document = new PdfDocument())
            {
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont28 = new PdfTrueTypeFont(fontStream, 28, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont18 = new PdfTrueTypeFont(fontStream, 18, PdfFontStyle.Bold);
                PdfTrueTypeFont BFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                // Configure page settings for an A4 landscape page
                PdfPageSettings pageSettings = new PdfPageSettings
                {
                    Size = PdfPageSize.A4,
                    Orientation = PdfPageOrientation.Landscape
                };
                #region 
                if (type == "1")
                {
                    // Add a section with the specified settings
                    PdfSection section = document.Sections.Add();
                    section.PageSettings = pageSettings;

                    // Add a page to the section
                    PdfPage page = section.Pages.Add();

                    // Get the graphics object to draw on the page
                    PdfGraphics graphics = page.Graphics;
                    // Load the background image
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "card", $"{setupsystem.certificate}");
                    using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        PdfBitmap backgroundImage = new PdfBitmap(imageStream);

                        // Draw the background image to cover the entire page
                        graphics.DrawImage(backgroundImage, 0, 0, page.Graphics.ClientSize.Width, page.Graphics.ClientSize.Height);
                    }
                    PdfBrush brush = PdfBrushes.Black;
                    PdfBrush brush1 = PdfBrushes.DarkBlue;
                    int yPostion = 230;

                    // Example: Add recipient's name
                    string recipientName = $"{name}"; // Replace with dynamic data
                    SizeF textSize = BFont28.MeasureString(recipientName);
                    float xPosition1 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    recipientName,
                                    BFont28,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition1 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        recipientName,
                        BFont28,
                        brush1,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion) // Centered horizontally
                    );
                    yPostion += 30;
                    // Example: Add award title
                    string schoolTitle = $"{SchoolName}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(schoolTitle);
                    float xPosition2 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    schoolTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition2 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        schoolTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    yPostion += 20;

                    // Example: Add award title
                    string awardTitle = $"{award}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(awardTitle);
                    float xPosition3 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    awardTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition3 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        awardTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    yPostion += 20;
                    // Example: Add award title
                    string CompetitionTitle = $"{ConvertToThaiDigits(RoleDescription)}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(CompetitionTitle);
                    float xPosition4 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    CompetitionTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition4 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        CompetitionTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    //
                    yPostion += 20;
                    // Example: Add award title
                    string locationTitle = $"{Location}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(locationTitle);
                    float xPosition5 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    locationTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition5 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        locationTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    ///เลขที่
                    // ข้อความของเลขที่เกียรติบัตร
                    var datacertificate = await _context.Certificate.Where(x => x.RegistrationID == id && x.RegistrationNo == idNo && x.type == type).FirstOrDefaultAsync();
                    string certificateNumber = ""; // แทนด้วยข้อมูลจริง
                    string gencode = "";
                    if (datacertificate != null)
                    {
                        // ถ้ามีข้อมูลแล้ว ดึง CertificateNumber มาใช้
                        certificateNumber = $"เลขที่เกียรติบัตร: {ConvertToThaiDigits(datacertificate.CertificateNumber)}";
                        gencode = datacertificate.CertificateNumber;
                    }
                    else
                    {
                        // ถ้ายังไม่มีข้อมูล ให้สร้างเลขที่เกียรติบัตรใหม่
                        string lastCertificateNumber = await _context.Certificate
                            .Where(x => x.SettingID == setupsystem.id)
                            .OrderByDescending(c => c.CertificateID)
                            .Select(c => c.CertificateNumber)
                            .FirstOrDefaultAsync();

                        // สร้างเลขที่เกียรติบัตรใหม่
                        if (string.IsNullOrEmpty(lastCertificateNumber))
                        {
                            lastCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/0000001"; // เริ่มต้นเลขใหม่
                            gencode = $"{setupsystem.time}.{setupsystem.yaer}/0000001";
                        }

                        string[] parts = lastCertificateNumber.Split('/');
                        int lastNumber = int.Parse(parts[1]);
                        int newNumber = lastNumber + 1;

                        string newCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
                        gencode = newCertificateNumber;
                        // บันทึกข้อมูลใหม่ลงฐานข้อมูล
                        var newCertificate = new Certificate
                        {
                            RegistrationID = id,
                            RegistrationNo = idNo,
                            SettingID = setupsystem.id,
                            AwardID = award,
                            AwardName = RoleDescription,
                            type = type,
                            Description = RoleDescription,
                            CertificateNumber = newCertificateNumber,
                            IssueDate = DateTime.Now,
                            lastupdate = DateTime.Now
                        };

                        _context.Certificate.Add(newCertificate);
                        await _context.SaveChangesAsync();

                        // แปลงเลขใน lastCertificateNumber
                        string thaiCertificateNumber = ConvertToThaiDigits(newCertificateNumber);
                        // ใช้เลขที่เกียรติบัตรใหม่
                        certificateNumber = $"เลขที่เกียรติบัตร: {thaiCertificateNumber}";
                    }


                    // คำนวณตำแหน่งสำหรับวางข้อความชิดขวา
                    SizeF textSizecertificateNumber = BFont16.MeasureString(certificateNumber); // วัดขนาดข้อความ
                    float xPosition = 20; // ชิดขวาโดยเว้นระยะขอบ 10 หน่วย
                    float yPosition = 10; // ระยะจากด้านบน


                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    certificateNumber,
                                    BFont16,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition + dx, yPosition + dy)
                                );
                            }
                        }
                    }
                    // วาดข้อความบนหน้า PDF
                    graphics.DrawString(
                        certificateNumber, // ข้อความ
                        BFont16,              // ฟอนต์
                        brush,             // แปรงสีสำหรับข้อความ
                        new PointF(xPosition, yPosition) // ตำแหน่งมุมบนขวา
                    );
                    ///barcode
                    ///
                    PdfQRBarcode qrbarcodelogo = new PdfQRBarcode();
                    qrbarcodelogo.InputMode = InputMode.BinaryMode;
                    // Automatically select the Version
                    // qrbarcodelogo.Version = QRCodeVersion.Auto;
                    // Set the Error correction level to high
                    qrbarcodelogo.ErrorCorrectionLevel = PdfErrorCorrectionLevel.High;
                    // Set dimension for each block
                    qrbarcodelogo.XDimension = 2;
                    qrbarcodelogo.Text = "https://korat.sillapas.com/Home/frmexamine?gencode=" + gencode;
                    //Set the logo image to QR barcode. 

                    using (FileStream imageStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                    {
                        Syncfusion.Pdf.Barcode.QRCodeLogo qRCodeLogo = new Syncfusion.Pdf.Barcode.QRCodeLogo(imageStream);
                        qrbarcodelogo.Logo = qRCodeLogo;
                        // Draw the QR barcode
                        qrbarcodelogo.Draw(page, new PointF(21, 465));
                    }
                }
                else if (type == "2")
                {
                    // Add a section with the specified settings
                    PdfSection section = document.Sections.Add();
                    section.PageSettings = pageSettings;

                    // Add a page to the section
                    PdfPage page = section.Pages.Add();

                    // Get the graphics object to draw on the page
                    PdfGraphics graphics = page.Graphics;
                    // Load the background image
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "card", $"{setupsystem.certificate}");
                    using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        PdfBitmap backgroundImage = new PdfBitmap(imageStream);

                        // Draw the background image to cover the entire page
                        graphics.DrawImage(backgroundImage, 0, 0, page.Graphics.ClientSize.Width, page.Graphics.ClientSize.Height);
                    }
                    PdfBrush brush = PdfBrushes.Black;
                    PdfBrush brush1 = PdfBrushes.DarkBlue;
                    int yPostion = 250;

                    // Example: Add recipient's name
                    string recipientName = $"{name}"; // Replace with dynamic data
                    SizeF textSize = BFont28.MeasureString(recipientName);
                    float xPosition1 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    recipientName,
                                    BFont28,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition1 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        recipientName,
                        BFont28,
                        brush1,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion) // Centered horizontally
                    );
                    yPostion += 30;
                    // Example: Add award title
                    string schoolTitle = $"{SchoolName}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(schoolTitle);
                    float xPosition2 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    schoolTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition2 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        schoolTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    yPostion += 20;

                    // Example: Add award title
                    string awardTitle = $"{RoleDescription}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(awardTitle);
                    float xPosition3 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    awardTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition3 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        awardTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    yPostion += 20;
                    // Example: Add award title
                    string CompetitionTitle = $"{Category}"; // Replace with dynamic data
                    textSize = BFont18.MeasureString(CompetitionTitle);
                    float xPosition4 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    CompetitionTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition4 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        CompetitionTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    //
                    yPostion += 20;
                    // Example: Add award title
                    string locationTitle = $""; // Replace with dynamic data
                    textSize = BFont18.MeasureString(locationTitle);
                    float xPosition5 = (page.GetClientSize().Width - textSize.Width) / 2;
                    // Draw the outline first (white border)
                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    locationTitle,
                                    BFont18,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition5 + dx, yPostion + dy)
                                );
                            }
                        }
                    }
                    graphics.DrawString(
                        locationTitle,
                        BFont18,
                        brush,
                        new PointF((page.GetClientSize().Width - textSize.Width) / 2, yPostion)
                    );
                    ///เลขที่
                    // ข้อความของเลขที่เกียรติบัตร
                    var datacertificate = await _context.Certificate.Where(x => x.RegistrationID == id && x.RegistrationNo == 1 && x.type == type).FirstOrDefaultAsync();
                    string certificateNumber = ""; // แทนด้วยข้อมูลจริง
                    string gencode = "";
                    if (datacertificate != null)
                    {
                        // ถ้ามีข้อมูลแล้ว ดึง CertificateNumber มาใช้
                        certificateNumber = $"เลขที่เกียรติบัตร: {ConvertToThaiDigits(datacertificate.CertificateNumber)}";
                        gencode = datacertificate.CertificateNumber;
                    }
                    else
                    {
                        // ถ้ายังไม่มีข้อมูล ให้สร้างเลขที่เกียรติบัตรใหม่
                        string lastCertificateNumber = await _context.Certificate
                            .Where(x => x.SettingID == setupsystem.id)
                            .OrderByDescending(c => c.CertificateID)
                            .Select(c => c.CertificateNumber)
                            .FirstOrDefaultAsync();

                        // สร้างเลขที่เกียรติบัตรใหม่
                        if (string.IsNullOrEmpty(lastCertificateNumber))
                        {
                            lastCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/0000001"; // เริ่มต้นเลขใหม่
                            gencode = $"{setupsystem.time}.{setupsystem.yaer}/0000001";
                        }

                        string[] parts = lastCertificateNumber.Split('/');
                        int lastNumber = int.Parse(parts[1]);
                        int newNumber = lastNumber + 1;

                        string newCertificateNumber = $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
                        gencode = newCertificateNumber;
                        // บันทึกข้อมูลใหม่ลงฐานข้อมูล
                        var newCertificate = new Certificate
                        {
                            RegistrationID = id,
                            RegistrationNo = 1,
                            SettingID = setupsystem.id,
                            AwardID = "กรรมการ",
                            AwardName = name,
                            type = type,
                            Description = RoleDescription,
                            CertificateNumber = newCertificateNumber,
                            IssueDate = DateTime.Now,
                            lastupdate = DateTime.Now
                        };

                        _context.Certificate.Add(newCertificate);
                        await _context.SaveChangesAsync();

                        // แปลงเลขใน lastCertificateNumber
                        string thaiCertificateNumber = ConvertToThaiDigits(newCertificateNumber);
                        // ใช้เลขที่เกียรติบัตรใหม่
                        certificateNumber = $"เลขที่เกียรติบัตร: {thaiCertificateNumber}";
                    }


                    // คำนวณตำแหน่งสำหรับวางข้อความชิดขวา
                    SizeF textSizecertificateNumber = BFont16.MeasureString(certificateNumber); // วัดขนาดข้อความ
                    float xPosition = 20; // ชิดขวาโดยเว้นระยะขอบ 10 หน่วย
                    float yPosition = 10; // ระยะจากด้านบน


                    for (float dx = -1; dx <= 1; dx++) // Horizontal offsets
                    {
                        for (float dy = -1; dy <= 1; dy++) // Vertical offsets
                        {
                            if (dx != 0 || dy != 0) // Skip the center point
                            {
                                graphics.DrawString(
                                    certificateNumber,
                                    BFont16,
                                    new PdfSolidBrush(Color.White), // White outline
                                    new PointF(xPosition + dx, yPosition + dy)
                                );
                            }
                        }
                    }
                    // วาดข้อความบนหน้า PDF
                    graphics.DrawString(
                        certificateNumber, // ข้อความ
                        BFont16,              // ฟอนต์
                        brush,             // แปรงสีสำหรับข้อความ
                        new PointF(xPosition, yPosition) // ตำแหน่งมุมบนขวา
                    );
                    ///barcode
                    ///
                    PdfQRBarcode qrbarcodelogo = new PdfQRBarcode();
                    qrbarcodelogo.InputMode = InputMode.BinaryMode;
                    // Automatically select the Version
                    // qrbarcodelogo.Version = QRCodeVersion.Auto;
                    // Set the Error correction level to high
                    qrbarcodelogo.ErrorCorrectionLevel = PdfErrorCorrectionLevel.High;
                    // Set dimension for each block
                    qrbarcodelogo.XDimension = 2;
                    qrbarcodelogo.Text = "https://korat.sillapas.com/Home/frmexamine?gencode=" + gencode;
                    //Set the logo image to QR barcode. 

                    using (FileStream imageStream = new FileStream($"wwwroot{setupsystem.LogoPath}", FileMode.Open, FileAccess.Read))
                    {
                        Syncfusion.Pdf.Barcode.QRCodeLogo qRCodeLogo = new Syncfusion.Pdf.Barcode.QRCodeLogo(imageStream);
                        qrbarcodelogo.Logo = qRCodeLogo;
                        // Draw the QR barcode
                        qrbarcodelogo.Draw(page, new PointF(21, 465));
                    }
                }
                #endregion

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"เกียรติบัตร.pdf");
                }
            }
        }
        public async Task<string> GenerateCertificateNumber(int settingId)
        {
            var setupsystem = await _context.setupsystem.Where(x => x.id == settingId).FirstOrDefaultAsync();
            // ดึงเลขที่เกียรติบัตรล่าสุดจากฐานข้อมูล
            string lastCertificateNumber = await _context.Certificate
                .OrderByDescending(c => c.CertificateID)
                .Select(c => c.CertificateNumber)
                .FirstOrDefaultAsync();

            // หากยังไม่มีเลขที่เกียรติบัตรในระบบ ให้เริ่มต้นใหม่
            if (string.IsNullOrEmpty(lastCertificateNumber))
            {
                return $"{setupsystem.time}.{setupsystem.yaer}/0000001";
            }

            // แยกส่วนตัวเลข 7 หลักสุดท้าย
            string[] parts = lastCertificateNumber.Split('/');
            int lastNumber = int.Parse(parts[1]);

            // เพิ่มตัวเลขสุดท้ายทีละ 1
            int newNumber = lastNumber + 1;

            // สร้างเลขที่เกียรติบัตรใหม่
            return $"{setupsystem.time}.{setupsystem.yaer}/{newNumber:D7}";
        }
        public async Task CreateCertificate(Certificate certificate, int settingId)
        {
            // สร้างเลขที่เกียรติบัตรใหม่
            certificate.CertificateNumber = await GenerateCertificateNumber(settingId);
            certificate.IssueDate = DateTime.Now;
            certificate.lastupdate = DateTime.Now;

            // บันทึกลงฐานข้อมูล
            _context.Certificate.Add(certificate);
            await _context.SaveChangesAsync();
        }
    }
}