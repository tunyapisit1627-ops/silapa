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
using AspNetCoreGeneratedDocument;
using Syncfusion.EJ2.Linq;
// Example usage of BouncyCastleFactoryCreator

namespace Silapa.Controllers
{
    public class PdfController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");
        public PdfController(ILogger<AdminController> logger, ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _env = env;
            // _configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GeneratePdf(int id, int g_id, string type, int r_id)
        {
            using (PdfDocument document = new PdfDocument())
            {
                var activeSettingIds = await _context.setupsystem
    .Where(s => s.status == "1")
    .Select(s => s.id)
    .ToListAsync();
                var setting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
                var filename = "บัตร";
                // เพิ่มหน้ากระดาษใหม่
                // PdfPage page = document.Pages.Add();
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont = new PdfTrueTypeFont(fontStream, 22, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont = new PdfTrueTypeFont(fontStream, 14, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont1 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont2 = new PdfTrueTypeFont(fontStream, 10, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont8 = new PdfTrueTypeFont(fontStream, 8, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont6 = new PdfTrueTypeFont(fontStream, 6, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont7 = new PdfTrueTypeFont(fontStream, 7, PdfFontStyle.Bold);
                var datarace = await _context.racedetails.Where(x => x.status == "1" && activeSettingIds.Contains(x.SettingID)).ToListAsync();
                if (type == "s")
                {

                    var urlnames = setting?.cardstudents ?? "";
                    var pathCards = _env.WebRootPath + "/card/" + urlnames;
                    var urlnamet = setting?.cardteacher ?? "";
                    var pathCardt = _env.WebRootPath + "/card/" + urlnamet;

                    // สร้าง Graphics สำหรับการเขียนลงใน PDF
                    //PdfGraphics graphics = page.Graphics;
                    FileStream docStreams = new FileStream(pathCards, FileMode.Open, FileAccess.Read);
                    FileStream docStreamt = new FileStream(pathCardt, FileMode.Open, FileAccess.Read);


                    var datah = await _context.Registerhead.Where(x => x.id == id && activeSettingIds.Contains(x.SettingID)).Include(x => x.Competitionlist).ThenInclude(x => x.racedetails).ThenInclude(x => x.Racelocation).Include(x => x.School).ThenInclude(x => x.grouplist).FirstOrDefaultAsync();
                    var datad = await _context.Registerdetail.Where(x => x.h_id == id).OrderBy(x => x.Type).ToListAsync();
                    var sqlrace = datarace.Where(x => x.c_id == datah?.c_id).FirstOrDefault();
                    var dd = GetCompetitionDetails(sqlrace, thaiCulture);
                    filename += "นักเรียนและครู-" + datah?.Competitionlist?.Name;
                    foreach (var dr in datad)
                    {
                        PdfLoadedDocument loadedDocument;
                        if (dr.Type == "student")
                        {
                            loadedDocument = new PdfLoadedDocument(docStreams);
                        }
                        else
                        {
                            loadedDocument = new PdfLoadedDocument(docStreamt);
                        }
                        PdfPageBase page = loadedDocument.Pages[0];
                        PdfGraphics graphics = page.Graphics;
                        string pathpic;
                        if (!string.IsNullOrEmpty(dr.ImageUrl))
                        {
                            pathpic = _env.WebRootPath + dr.ImageUrl;
                        }
                        else
                        {
                            // ใช้ภาพเริ่มต้นถ้าไม่มี ImageUrl
                            pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                        }
                        if (!System.IO.File.Exists(pathpic))
                        {
                            pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                        }
                        FileStream imageStream = new FileStream(pathpic, FileMode.Open, FileAccess.Read);
                        PdfBitmap image1 = new PdfBitmap(imageStream);
                        // กำหนดตำแหน่งและขนาด
                        // กำหนดตำแหน่งและขนาดของรูปภาพ
                        float x = 38;
                        float y = 52;
                        float width = 100;
                        float height = 100;
                        // สีและขนาดของขอบ
                        float borderWidth = 2; // ความหนาของขอบ
                        PdfPen borderPen = new PdfPen(Syncfusion.Drawing.Color.White, borderWidth); // สีขอบ

                        // วาดรูปภาพ
                        graphics.DrawImage(image1, x, y, width, height);

                        // วาดสี่เหลี่ยมรอบรูปภาพเป็นขอบ
                        graphics.DrawRectangle(borderPen, x - borderWidth / 2, y - borderWidth / 2, width + borderWidth, height + borderWidth);

                        // Create the QR Barcode
                        PdfQRBarcode barcode = new PdfQRBarcode
                        {
                            ErrorCorrectionLevel = PdfErrorCorrectionLevel.High,
                            XDimension = 0.36f,
                            Text = "https://silpa.in.th/" // The data to encode
                        };
                        barcode.Draw(graphics, new PointF(145, 188)); // Adjust position as needed


                        // วาดข้อความเพิ่มเติม
                        // ข้อความที่ต้องการแสดง
                        string fullName = $"{dr.Prefix}{dr.FirstName} {dr.LastName}";

                        // คำนวณขนาดของข้อความ
                        SizeF textSize = bFont1.MeasureString(fullName);



                        // คำนวณตำแหน่งให้อยู่ตรงกลางหน้า
                        float centerX = (page.Size.Width - textSize.Width) / 2;
                        float yPosition = 190; // ตำแหน่ง Y ตามที่ต้องการ

                        // กำหนดความกว้างของพื้นที่ข้อความที่ต้องการให้จัดกึ่งกลาง
                        float pageWidth = page.Graphics.ClientSize.Width;

                        // สร้าง RectangleF เพื่อระบุตำแหน่งที่จะวาดข้อความ
                        RectangleF rect = new RectangleF(0, yPosition, pageWidth, bFont2.Height);
                        RectangleF rect1 = new RectangleF(0, yPosition += 10, pageWidth, bFont8.Height);

                        // ใช้ PdfStringFormat เพื่อกำหนดให้ข้อความจัดกึ่งกลางทั้งแนวนอนและแนวตั้ง
                        PdfStringFormat centerAlignment = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle,

                        };
                        float lineHeight = bFont8.Height;

                        // วาดข้อความตรงกลาง
                        graphics.DrawString(fullName, bFont2, PdfBrushes.Black, rect, centerAlignment);
                        graphics.DrawString($"{datah?.School.Name} {datah?.School.grouplist?.Name}", bFont8, PdfBrushes.Black, rect1, centerAlignment);
                        float horizontalMargin = 20; // ระยะห่างจากขอบ (คุณสามารถปรับเลขนี้ได้)
                        float totalDrawWidth = pageWidth - (2 * horizontalMargin);

                        string text1 = $"{datah?.Competitionlist?.Name}";
                        float maxWidth = totalDrawWidth; // ⬅️ ให้ใช้ TotalDrawWidth ในการ Split
                        yPosition += bFont8.Height;
                        float currentY = yPosition;

                        // แบ่งข้อความถ้ากว้างเกิน
                        string[] lines1 = SplitTextToFitWidth(text1, graphics, bFont8, maxWidth);

                        foreach (string line in lines1)
                        {
                            // 🚨 FIX: เริ่มต้นที่ขอบซ้าย (horizontalMargin) และจำกัดความกว้าง
                            RectangleF rectLine = new RectangleF(horizontalMargin, currentY, totalDrawWidth, bFont8.Height);

                            // (คำสั่ง DrawString จะจัดกึ่งกลางภายใน rectLine นี้)
                            graphics.DrawString(line, bFont8, PdfBrushes.Black, rectLine, centerAlignment);
                            currentY += lineHeight;
                        }

                        // ปรับ yPosition สำหรับเนื้อหาถัดไป
                        yPosition = currentY;
                        string text = $"{dd}"; // ข้อความที่ต้องการวาด

                        RectangleF rect4 = new RectangleF(20, yPosition, 120, 200);
                        List<string> lines = WrapText1(text, bFont6, rect4.Width, graphics);

                        // คำนวณความสูงของข้อความทั้งหมด
                        float totalTextHeight = lines.Count * lineHeight;

                        // คำนวณตำแหน่งเริ่มต้น Y เพื่อให้ข้อความอยู่ตรงกลางแนวตั้ง
                        float startY = rect4.Y;

                        // เริ่มวาดข้อความ
                        foreach (string line in lines)
                        {
                            // คำนวณตำแหน่ง X เพื่อให้ข้อความอยู่ตรงกลางแนวนอน
                            float lineWidth = bFont6.MeasureString(line).Width;
                            float startX = rect4.X + (rect4.Width - lineWidth) / 2;

                            // วาดข้อความในตำแหน่งที่กำหนด
                            graphics.DrawString(line, bFont6, PdfBrushes.Black, new PointF(rect4.X, startY));
                            startY += lineHeight;

                            // หากเกินพื้นที่ที่กำหนดให้หยุด
                            if (startY > rect4.Y + rect4.Height)
                                break;
                        }
                        document.Append(loadedDocument);
                    }
                }
                else if (type == "r")
                {
                    var urlname = setting.cardreferee ?? "";
                    var pathCard = _env.WebRootPath + "/card/" + urlname;

                    // สร้าง Graphics สำหรับการเขียนลงใน PDF
                    //PdfGraphics graphics = page.Graphics;
                    FileStream docStream = new FileStream(pathCard, FileMode.Open, FileAccess.Read);
                    var data = await _context.referee.Where(x => x.c_id == id && x.status == "1" && activeSettingIds.Contains(x.SettingID)).Include(x => x.Competitionlist).ThenInclude(x => x.racedetails).ThenInclude(x => x.Racelocation).ToListAsync();
                    if (r_id != 0)
                    {
                        data = data.Where(x => x.id == r_id).ToList();
                    }
                    //  var datadd = await _context.racedetails.Where(x => x.c_id == data[0].id).Include(x => x.Racelocation).FirstOrDefaultAsync(); //data?.FirstOrDefault()?.Competitionlist?.racedetails ?? new List<racedetails>();
                    var datadd = datarace.Where(x => x.c_id == id).FirstOrDefault();

                    var dd = GetCompetitionDetails(datadd, thaiCulture);
                    filename += "กรรมการ-" + data.FirstOrDefault().Competitionlist.Name;
                    foreach (var dr in data)
                    {
                        PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);
                        PdfPageBase page = loadedDocument.Pages[0];
                        PdfGraphics graphics = page.Graphics;
                        string pathpic;
                        if (!string.IsNullOrEmpty(dr.ImageUrl))
                        {
                            pathpic = _env.WebRootPath + dr.ImageUrl;
                        }
                        else
                        {
                            // ใช้ภาพเริ่มต้นถ้าไม่มี ImageUrl
                            pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                        }
                        try
                        {
                            if (!System.IO.File.Exists(pathpic))
                            {
                                pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                            }
                            FileStream imageStream = new FileStream(pathpic, FileMode.Open, FileAccess.Read);
                            PdfBitmap image1 = new PdfBitmap(imageStream);
                            // กำหนดตำแหน่งและขนาด
                            // กำหนดตำแหน่งและขนาดของรูปภาพ
                            float x = 38;
                            float y = 52;
                            float width = 100;
                            float height = 100;
                            // สีและขนาดของขอบ
                            float borderWidth = 2; // ความหนาของขอบ
                            PdfPen borderPen = new PdfPen(Syncfusion.Drawing.Color.White, borderWidth); // สีขอบ

                            // วาดรูปภาพ
                            graphics.DrawImage(image1, x, y, width, height);

                            // วาดสี่เหลี่ยมรอบรูปภาพเป็นขอบ
                            graphics.DrawRectangle(borderPen, x - borderWidth / 2, y - borderWidth / 2, width + borderWidth, height + borderWidth);

                            // Create the QR Barcode
                            PdfQRBarcode barcode = new PdfQRBarcode
                            {
                                ErrorCorrectionLevel = PdfErrorCorrectionLevel.High,
                                XDimension = 0.36f,
                                Text = "https://silpa.in.th/" // The data to encode
                            };
                            barcode.Draw(graphics, new PointF(145, 188)); // Adjust position as needed

                        }
                        catch { }


                        // วาดข้อความเพิ่มเติม
                        // ข้อความที่ต้องการแสดง
                        string fullName = dr.name;

                        // คำนวณขนาดของข้อความ
                        SizeF textSize = bFont1.MeasureString(fullName);

                        // คำนวณตำแหน่งให้อยู่ตรงกลางหน้า
                        float centerX = (page.Size.Width - textSize.Width) / 2;
                        float yPosition = 190; // ตำแหน่ง Y ตามที่ต้องการ

                        // กำหนดความกว้างของพื้นที่ข้อความที่ต้องการให้จัดกึ่งกลาง
                        float pageWidth = page.Graphics.ClientSize.Width;

                        // สร้าง RectangleF เพื่อระบุตำแหน่งที่จะวาดข้อความ
                        RectangleF rect = new RectangleF(0, yPosition, pageWidth, bFont2.Height);
                        RectangleF rect1 = new RectangleF(0, yPosition += 10, pageWidth, bFont8.Height);

                        // ใช้ PdfStringFormat เพื่อกำหนดให้ข้อความจัดกึ่งกลางทั้งแนวนอนและแนวตั้ง
                        PdfStringFormat centerAlignment = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle,

                        };
                        float lineHeight = bFont8.Height;

                        // วาดข้อความในแนวนอน (กึ่งกลาง)
                        graphics.DrawString(fullName, bFont2, PdfBrushes.Black, rect, centerAlignment);
                        graphics.DrawString($"{dr.position}", bFont6, PdfBrushes.Black, rect1, centerAlignment);

                        float horizontalMargin = 20;
                        float totalDrawWidth = pageWidth - (2 * horizontalMargin);
                        string text1 = $"{dr.Competitionlist?.Name}";
                        float maxWidth = totalDrawWidth; // ⬅️ ให้ใช้ TotalDrawWidth ในการ Split
                        yPosition += bFont8.Height;
                        float currentY = yPosition;

                        // แบ่งข้อความถ้ากว้างเกิน
                        string[] lines1 = SplitTextToFitWidth(text1, graphics, bFont8, maxWidth);

                        foreach (string line in lines1)
                        {
                            // 🚨 FIX: เริ่มต้นที่ขอบซ้าย (horizontalMargin) และจำกัดความกว้าง
                            RectangleF rectLine = new RectangleF(horizontalMargin, currentY, totalDrawWidth, bFont8.Height);

                            // (คำสั่ง DrawString จะจัดกึ่งกลางภายใน rectLine นี้)
                            graphics.DrawString(line, bFont8, PdfBrushes.Black, rectLine, centerAlignment);
                            currentY += lineHeight;
                        }

                        yPosition = currentY;
                        // ข้อความที่ต้องการวาด
                        string text = $"{dd}";
                        RectangleF rect4 = new RectangleF(20, yPosition, 120, 200);
                        // แบ่งข้อความเป็นบรรทัดๆ โดยใช้ WrapText1
                        List<string> lines = WrapText1(text, bFont6, rect4.Width, graphics);

                        // คำนวณความสูงของข้อความทั้งหมด
                        float totalTextHeight = lines.Count * lineHeight;

                        // คำนวณตำแหน่งเริ่มต้น Y เพื่อให้ข้อความอยู่ตรงกลางแนวตั้ง
                        float startY = rect4.Y;

                        // เริ่มวาดข้อความ
                        foreach (string line in lines)
                        {
                            // คำนวณตำแหน่ง X เพื่อให้ข้อความอยู่ตรงกลางแนวนอน
                            float lineWidth = bFont6.MeasureString(line).Width;
                            float startX = rect4.X + (rect4.Width - lineWidth) / 2;

                            // วาดข้อความในตำแหน่งที่กำหนด
                            graphics.DrawString(line, bFont6, PdfBrushes.Black, new PointF(rect4.X, startY));
                            startY += lineHeight;

                            // หากเกินพื้นที่ที่กำหนดให้หยุด
                            if (startY > rect4.Y + rect4.Height)
                                break;
                        }
                        ///บันทึกลงบัตร
                        document.Append(loadedDocument);
                    }
                }
                else if (type == "rr")
                {
                    var urlname = setting.carddirector ?? "";
                    var pathCard = _env.WebRootPath + "/card/" + urlname;

                    // สร้าง Graphics สำหรับการเขียนลงใน PDF
                    //PdfGraphics graphics = page.Graphics;
                    FileStream docStream = new FileStream(pathCard, FileMode.Open, FileAccess.Read);
                    var data = await _context.referee.Where(x => x.m_id == id && x.g_id == g_id && x.status == "1" && activeSettingIds.Contains(x.SettingID))
                    .Include(x => x.Groupreferee)
                    .AsNoTracking()
                    .ToListAsync();
                    if (r_id != 0)
                    {
                        data = data.Where(x => x.id == r_id).ToList();
                    }
                    //  var datadd = await _context.racedetails.Where(x => x.c_id == data[0].id).Include(x => x.Racelocation).FirstOrDefaultAsync(); //data?.FirstOrDefault()?.Competitionlist?.racedetails ?? new List<racedetails>();
                    var datacategory = await _context.category.Where(x => x.Id == id).FirstOrDefaultAsync();


                    filename += "กรรมการดำเนินการ-" + datacategory.Name ?? "";
                    foreach (var dr in data)
                    {
                        PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);
                        PdfPageBase page = loadedDocument.Pages[0];
                        PdfGraphics graphics = page.Graphics;
                        string pathpic;
                        if (!string.IsNullOrEmpty(dr.ImageUrl))
                        {
                            pathpic = _env.WebRootPath + dr.ImageUrl;
                        }
                        else
                        {
                            // ใช้ภาพเริ่มต้นถ้าไม่มี ImageUrl
                            pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                        }
                        try
                        {
                            if (!System.IO.File.Exists(pathpic))
                            {
                                pathpic = _env.WebRootPath + "/images/no-image-icon-4.png"; // กำหนดเส้นทางภาพเริ่มต้น
                            }
                            FileStream imageStream = new FileStream(pathpic, FileMode.Open, FileAccess.Read);
                            PdfBitmap image1 = new PdfBitmap(imageStream);
                            // กำหนดตำแหน่งและขนาด
                            // กำหนดตำแหน่งและขนาดของรูปภาพ
                            float x = 38;
                            float y = 52;
                            float width = 100;
                            float height = 100;
                            // สีและขนาดของขอบ
                            float borderWidth = 2; // ความหนาของขอบ
                            PdfPen borderPen = new PdfPen(Syncfusion.Drawing.Color.White, borderWidth); // สีขอบ

                            // วาดรูปภาพ
                            graphics.DrawImage(image1, x, y, width, height);

                            // วาดสี่เหลี่ยมรอบรูปภาพเป็นขอบ
                            graphics.DrawRectangle(borderPen, x - borderWidth / 2, y - borderWidth / 2, width + borderWidth, height + borderWidth);



                            // Create the QR Barcode
                            PdfQRBarcode barcode = new PdfQRBarcode
                            {
                                ErrorCorrectionLevel = PdfErrorCorrectionLevel.High,
                                XDimension = 0.36f,
                                Text = "https://silpa.in.th/" // The data to encode
                            };
                            barcode.Draw(graphics, new PointF(145, 188)); // Adjust position as needed

                        }
                        catch { }


                        // วาดข้อความเพิ่มเติม
                        // ข้อความที่ต้องการแสดง
                        string fullName = dr.name;

                        // คำนวณขนาดของข้อความ
                        SizeF textSize = bFont1.MeasureString(fullName);

                        // คำนวณตำแหน่งให้อยู่ตรงกลางหน้า
                        float centerX = (page.Size.Width - textSize.Width) / 2;
                        float yPosition = 200; // ตำแหน่ง Y ตามที่ต้องการ

                        // กำหนดความกว้างของพื้นที่ข้อความที่ต้องการให้จัดกึ่งกลาง
                        float pageWidth = page.Graphics.ClientSize.Width;

                        // สร้าง RectangleF เพื่อระบุตำแหน่งที่จะวาดข้อความ
                        RectangleF rect = new RectangleF(0, yPosition, pageWidth, bFont2.Height);
                        RectangleF rect1 = new RectangleF(0, yPosition += 10, pageWidth, bFont8.Height);
                        RectangleF rect2 = new RectangleF(0, yPosition += 10, pageWidth, bFont8.Height);
                        RectangleF rect3 = new RectangleF(0, yPosition += 10, pageWidth, bFont8.Height);
                        RectangleF rect4 = new RectangleF(0, yPosition + 10, pageWidth, bFont8.Height);

                        // ใช้ PdfStringFormat เพื่อกำหนดให้ข้อความจัดกึ่งกลางทั้งแนวนอนและแนวตั้ง
                        PdfStringFormat centerAlignment = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle
                        };
                        float lineHeight = bFont8.Height - 5; // ความสูงของแต่ละบรรทัด


                        // วาดข้อความในแนวนอน (กึ่งกลาง)
                        graphics.DrawString(fullName, bFont2, PdfBrushes.Black, rect, centerAlignment);
                        graphics.DrawString($"{dr.position}", bFont8, PdfBrushes.Black, rect1, centerAlignment);
                        graphics.DrawString($"{dr.role}", bFont8, PdfBrushes.Black, rect2, centerAlignment);
                        graphics.DrawString($"{dr.Groupreferee.name}", bFont8, PdfBrushes.Black, rect3, centerAlignment);
                        if (dr.g_id != 33 && dr.g_id != 26)
                        {
                            string text1 = $"{datacategory.Name}";
                            float maxWidth = pageWidth - 20; // ลดขอบซ้ายและขวา
                            yPosition += bFont8.Height;
                            float currentY = yPosition; // เริ่มจากตำแหน่ง Y ปัจจุบัน

                            // แบ่งข้อความถ้ากว้างเกิน
                            string[] lines1 = SplitTextToFitWidth(text1, graphics, bFont8, maxWidth);

                            foreach (string line in lines1)
                            {
                                RectangleF rectLine = new RectangleF(0, currentY, pageWidth, bFont2.Height);
                                graphics.DrawString(line, bFont8, PdfBrushes.Black, rectLine, centerAlignment);
                                currentY += lineHeight; // ขยับตำแหน่ง Y สำหรับบรรทัดถัดไป
                            }

                            // ปรับ yPosition สำหรับเนื้อหาถัดไป
                            yPosition = currentY;
                        }
                        /* if (id == 31)
                         {
                             graphics.DrawString($"{datacategory.Name}", bFont8, PdfBrushes.Black, rect4, centerAlignment);
                         }
                         else
                         {
                             graphics.DrawString($"ศูนย์{datacategory.Name}", bFont8, PdfBrushes.Black, rect4, centerAlignment);
                         }*/


                        // ข้อความที่ต้องการวาด
                        //  string text1 = $"{dr.Competitionlist.Name}\n{dd}";


                        ///บันทึกลงบัตร
                        document.Append(loadedDocument);
                    }
                }



                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", $"{filename}.pdf");
                }
            }

        }
        [HttpGet]
        public async Task<IActionResult> GenerateListPdf(int id, int type)
        {
            //System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");
            using (PdfDocument document = new PdfDocument())
            {
                var roleOrder = new Dictionary<string, int>
{
    { "ประธาน", 1 },
    { "กรรมการ", 2 },
    { "กรรมการและเลขานุการ", 3 }
};
                // เพิ่มหน้ากระดาษใหม่
                // PdfPage page = document.Pages.Add();
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                var urlname = "Cardstudents.pdf";
                var pathCard = _env.WebRootPath + "/card/" + urlname;
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont = new PdfTrueTypeFont(fontStream, 22, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont1 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Bold);
                PdfTrueTypeFont BFontbFont2 = new PdfTrueTypeFont(fontStream, 10, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont8 = new PdfTrueTypeFont(fontStream, 8, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont = new PdfTrueTypeFont(fontStream, 14, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfSolidBrush brush = new PdfSolidBrush(Color.Black);
                // เพิ่มหน้าใหม่ในเอกสาร
                PdfPage page = document.Pages.Add();
                var activeSettingIds = await _context.setupsystem
    .Where(s => s.status == "1")
    .Select(s => s.id)
    .ToListAsync();
                // PdfGraphics graphics = page.Graphics;
                var datasetting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
                var datacom = await _context.Competitionlist.Where(x => x.Id == id).Include(x => x.racedetails).FirstOrDefaultAsync();
                var daterace = datacom.racedetails.Where(x => x.c_id == id && activeSettingIds.Contains(x.SettingID)).FirstOrDefault();
                string namejob = $"{datasetting?.name} {datasetting?.ProvinceName} ครั้งที่ {datasetting?.time} ปีการศึกษา {datasetting?.yaer}";
                string formattedDateRange = "";
                string time = "";
                if (daterace != null && daterace.daterace != null)
                {
                    formattedDateRange = DateHelper.ConvertToBuddhistDateRange(daterace.daterace, thaiCulture);
                    time = daterace.time;
                }
                else
                {
                    formattedDateRange = "ไม่ระบุวันที่";
                }
                // string fullName = "เอกสารการแข่งขันงานศิลปหัตถกรรมนักเรียน";
                // คำนวณตำแหน่งข้อความให้อยู่กึ่งกลางแนวนอน
                float pageWidth = page.GetClientSize().Width;
                string text1 = "เอกสารการแข่งขันงานศิลปหัตถกรรมนักเรียน";
                string text2 = $"{datasetting.name}\nกิจกรรม {datacom.Name}\n แข่งขันวันที่ {formattedDateRange} เวลา {time}";

                string text3 = "1. เอกสารภายในซองประกอบด้วย\n" +
              " - เอกสารลงทะเบียนนักเรียน (DOC.1)\n" +
              " - เอกสารลงทะเบียนครูผู้สอน (DOC.2)\n" +
              " - เอกสารลงทะเบียนกรรมการตัดสินผลการแข่งขัน (DOC.3)\n" +
              " - เอกสารการบันทึกคะแนน (DOC.4)\n" +
              " - เอกสารแก้ไขชื่อ-สกุล / เปลี่ยนตัว นักเรียน (DOC.5)\n" +
              " - เกณฑ์การแข่งขันทักษะและเอกสารอื่น ๆ(ถ้ามี)\n" +
              " - ข้อสอบ / โจทย์ การแข่งขัน(ถ้ามี)\n" +
              "2. นักเรียนลงทะเบียนเข้าแข่งขัน(DOC.1) โดยให้นักเรียนตรวจสอบ ชื่อ - สกุล ถ้าพบข้อผิดพลาดหรือเปลี่ยนตัว\n" +
              " ให้ขีดบริเวณที่ผิด แล้วแก้ไขโดยเขียน ชื่อ - สกุลให้ถูกต้อง และเขียนชื่อตนเองตัวบรรจง ลงในเอกสารการทะเบียน DOC.1\n" +
              "แล้วเขียน ชื่อ-สกุล ใหม่ลงใน DOC.5 เอกสารแก้ไขชื่อ-สกุล / เปลี่ยนตัว นักเรียน (เฉพาะนักเรียนที่ ชื่อ-สกุลไม่ถูกต้องหรือเปลี่ยนตัว)\n" +
              " สำหรับนักเรียนที่มาลงทะเบียนใหม่ ให้ลงชื่อ-สกุล ต่อท้ายเอกสาร DOC.1\n" +
              "3. ครูผู้สอนลงทะเบียน(DOC.2) โดยให้ครูผู้สอนเขียนชื่อตนเองตัวบรรจง และ ให้ตรวจสอบ ชื่อ - สกุล\n" +
              " ถ้าพบข้อผิดพลาด ให้ทำเช่นเดียวกันกับข้อ 2\n" +
              "4. กรรมการตัดสินผลการแข่งขัน(DOC.3) โดยให้กรรมการตรวจสอบ ชื่อ - สกุล ถ้าพบข้อผิดพลาด\n" +
              " ให้ดำเนินการเช่นเดียวกับข้อ 2 แล้วเซ็นชื่อลง เวลามา เวลากลับ และเขียนชื่อตนเองตัวบรรจง\n" +
              "5. ตัดสินผลการแข่งขันและบันทึกคะแนน(DOC.4) ให้บันทึกคะแนนรวม 100 คะแนน ลงในเอกสารบันทึกคะแนน(DOC.4)\n" +
              " จะเป็นจำนวนเต็มหรือทศนิยมได้ไม่เกิน 3 ตำแหน่ง และ กรรมการทุกท่านเซ็นต์ชื่อรับรองผลคะแนน\n" +
              "6. นำเอกสารทั้งหมด ใส่ซองตามเดิม นำส่ง ผู้รับผิดชอบ เพื่อบันทึก คะแนนการแข่งขันและรายงานผลการแข่งขันต่อไป";

                // คำนวณความกว้างของข้อความ
                float xCenter = (pageWidth - bFont.MeasureString(text1).Width) / 2;
                float yPosition = 100; // ตำแหน่งแนวตั้งเริ่มต้น

                // เขียนข้อความลงในหน้า PDF พร้อมจัดกึ่งกลาง
                page.Graphics.DrawString(text1, bFont, PdfBrushes.Black, new PointF(xCenter, yPosition));
                yPosition += 20; // เพิ่มตำแหน่งแนวตั้งเพื่อเว้นบรรทัด

                RectangleF bounds = new RectangleF(0, yPosition, page.GetClientSize().Width, page.GetClientSize().Height - 100);
                DrawTextWithWrapping(page, text2, bFont, PdfBrushes.Black, bounds);
                yPosition += 80;
                RectangleF bounds1 = new RectangleF(0, yPosition, page.GetClientSize().Width, page.GetClientSize().Height - 100);
                DrawTextWithWrappingLeft(page, text3, bFont1, PdfBrushes.Black, bounds1);
                yPosition += 20;

                //ดึงข้อมูลมา
                var data = await _context.Registerhead.Where(x => x.c_id == id && x.status == "1" && activeSettingIds.Contains(x.SettingID)).Include(x => x.Registerdetail).Include(x => x.School).ThenInclude(x => x.grouplist).ToListAsync();
                if (type == 1)
                {
                    data.OrderBy(x => x.School.grouplist.Id);
                }
                else
                {
                    data.OrderBy(x => x.id);
                }
                // 1. กำหนดตัวแปร Path เริ่มต้น
                string logoPhysicalPath = "";
                // ตรวจสอบว่ามีข้อมูลใน DB หรือไม่
                if (datasetting != null && !string.IsNullOrEmpty(datasetting.LogoPath))
                {
                    // ⚡️ แก้ไข: จัดการเครื่องหมาย Slash ให้ถูกต้องตาม OS (Windows/Linux)
                    // เปลี่ยน / เป็น \ (ถ้าเป็น Windows) และลบตัวหน้าสุดออก
                    string cleanPath = datasetting.LogoPath
                        .Replace("/", Path.DirectorySeparatorChar.ToString())
                        .Replace("\\", Path.DirectorySeparatorChar.ToString())
                        .TrimStart(Path.DirectorySeparatorChar);

                    logoPhysicalPath = Path.Combine(_env.WebRootPath, cleanPath);
                }

                // 2. ตรวจสอบไฟล์ (ถ้าไม่มีไฟล์จริง ให้ใช้ Default)
                if (string.IsNullOrEmpty(logoPhysicalPath) || !System.IO.File.Exists(logoPhysicalPath))
                {
                    // ใช้รูปสำรอง (AdminLTE)
                    // ใช้ Path.Combine เพื่อต่อ Path ให้ถูกต้องแน่นอน
                    logoPhysicalPath = Path.Combine(_env.WebRootPath, "dist", "img", "AdminLTELogo.png");
                }
                ///หน้า2 doc1
                ///
                for (int i = 0; i < 3; i++)
                {
                    if (i == 0 || i == 1)
                    {
                        yPosition = 70;
                        PdfPage page2 = document.Pages.Add();
                        PdfGraphics graphics = page2.Graphics;
                        // 3. เริ่มวาดโลโก้
                        if (System.IO.File.Exists(logoPhysicalPath))
                        {
                            try
                            {
                                using (FileStream logoStream = new FileStream(logoPhysicalPath, FileMode.Open, FileAccess.Read))
                                {
                                    PdfBitmap logoImage = new PdfBitmap(logoStream);

                                    float logoWidth = 75;
                                    float logoHeight = 75;
                                    float logoX = (pageWidth - logoWidth) / 2;
                                    float logoY = 0;

                                    page.Graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                                    graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                                }
                            }
                            catch
                            {
                                // (ซ่อน Error กรณีไฟล์รูปมีปัญหา)
                            }
                        }

                        string formattedText = WrapText(namejob, 100);
                        RectangleF bound2_1 = new RectangleF(20, yPosition, page.GetClientSize().Width, page.GetClientSize().Height - 100);
                        DrawTextWithWrapping(page2, formattedText, bFont, PdfBrushes.Black, bound2_1);
                        yPosition += 40;
                        var typename = "";
                        if (i == 0)
                        {
                            typename = "student";
                            graphics.DrawString("DOC.1 ", bFont, PdfBrushes.Black, new PointF(0, 0));
                            graphics.DrawString("แบบลงทะเบียนนักเรียน " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition));
                        }
                        else
                        {
                            typename = "teacher";
                            graphics.DrawString("DOC.2 ", bFont, PdfBrushes.Black, new PointF(0, 0));
                            graphics.DrawString("แบบลงทะเบียนครู " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition));
                        }


                        // สร้างตาราง
                        PdfLightTable table = new PdfLightTable();
                        //Create a DataTable.
                        DataTable dataTable = new DataTable();
                        dataTable.Columns.Add("ลำดับ");
                        dataTable.Columns.Add("โรงเรียน");
                        dataTable.Columns.Add("กลุ่ม");
                        dataTable.Columns.Add("รายชื่อ");
                        dataTable.Columns.Add("ลงชื่อตัวบรรจง");

                        // เพิ่มข้อมูลใน DataTable
                        int index = 1;
                        foreach (var item in data.OrderBy(x => x.School.Name))
                        {
                            int y = 1;
                            string studentNames = "";
                            string signatureLines = "";

                            // ตรวจสอบว่า `item.Registerdetail` มีข้อมูลหรือไม่
                            if (item.Registerdetail != null)
                            {
                                // วนลูปรายชื่อนักเรียนที่ตรงตามเงื่อนไข
                                foreach (var dr in item.Registerdetail.Where(x => x.h_id == item.id && x.Type == typename))
                                {
                                    // ตรวจสอบว่าข้อมูลแต่ละฟิลด์ไม่เป็น null
                                    string prefix = dr.Prefix ?? "";
                                    string firstName = dr.FirstName ?? "";
                                    string lastName = dr.LastName ?? "";

                                    // ต่อข้อความ
                                    studentNames += $"{y}. {prefix}{firstName} {lastName}{Environment.NewLine}";
                                    signatureLines += $"{y}.{Environment.NewLine}";
                                    y++;
                                }
                            }

                            // ตรวจสอบข้อมูลก่อนเพิ่มใน DataTable
                            dataTable.Rows.Add(
                                index.ToString(),                             // ลำดับ
                                item.School?.Name ?? "ไม่ระบุ",               // โรงเรียน
                                item.School?.grouplist?.Name ?? "ไม่ระบุ",   // กลุ่ม
                                studentNames.TrimEnd(),                      // รายชื่อนักเรียน
                                signatureLines.TrimEnd()                     // ลงชื่อตัวบรรจง
                            );

                            index++;
                        }
                        table.DataSource = dataTable;
                        table.Columns[0].Width = 30;
                        table.Columns[1].Width = 160;
                        table.Columns[2].Width = 50;
                        table.Columns[3].Width = 160;
                        PdfCellStyle defStyle = new PdfCellStyle();
                        defStyle.Font = ttFont;
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
                        table.Columns[3].StringFormat = format11;
                        table.Columns[4].StringFormat = format11;


                        // วาดตารางในเอกสาร PDF
                        table.Draw(page2, new PointF(0, 130));
                    }
                    else
                    {
                        yPosition = 70;
                        PdfPage page2 = document.Pages.Add();
                        PdfGraphics graphics = page2.Graphics;
                        // 1. กำหนดตัวแปร Path เริ่มต้น
                        string logoPhysicalPath1 = "";

                        // ตรวจสอบว่ามีข้อมูลใน DB หรือไม่
                        if (datasetting != null && !string.IsNullOrEmpty(datasetting.LogoPath))
                        {
                            // ⚡️ แก้ไข: จัดการเครื่องหมาย Slash ให้ถูกต้องตาม OS (Windows/Linux)
                            // เปลี่ยน / เป็น \ (ถ้าเป็น Windows) และลบตัวหน้าสุดออก
                            string cleanPath = datasetting.LogoPath
                                .Replace("/", Path.DirectorySeparatorChar.ToString())
                                .Replace("\\", Path.DirectorySeparatorChar.ToString())
                                .TrimStart(Path.DirectorySeparatorChar);

                            logoPhysicalPath1 = Path.Combine(_env.WebRootPath, cleanPath);
                        }

                        // 2. ตรวจสอบไฟล์ (ถ้าไม่มีไฟล์จริง ให้ใช้ Default)
                        if (string.IsNullOrEmpty(logoPhysicalPath1) || !System.IO.File.Exists(logoPhysicalPath1))
                        {
                            // ใช้รูปสำรอง (AdminLTE)
                            // ใช้ Path.Combine เพื่อต่อ Path ให้ถูกต้องแน่นอน
                            logoPhysicalPath1 = Path.Combine(_env.WebRootPath, "dist", "img", "AdminLTELogo.png");
                        }
                        // 3. เริ่มวาดโลโก้
                        if (System.IO.File.Exists(logoPhysicalPath1))
                        {
                            try
                            {
                                using (FileStream logoStream = new FileStream(logoPhysicalPath1, FileMode.Open, FileAccess.Read))
                                {
                                    PdfBitmap logoImage = new PdfBitmap(logoStream);

                                    float logoWidth = 75;
                                    float logoHeight = 75;
                                    float logoX = (pageWidth - logoWidth) / 2;
                                    float logoY = 0;
                                    graphics.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                                }
                            }
                            catch
                            {
                                // (ซ่อน Error กรณีไฟล์รูปมีปัญหา)
                            }
                        }
                        string formattedText = WrapText(namejob, 100);
                        RectangleF bound2_1 = new RectangleF(20, yPosition, page.GetClientSize().Width, page.GetClientSize().Height - 100);
                        DrawTextWithWrapping(page2, formattedText, bFont, PdfBrushes.Black, bound2_1);
                        yPosition += 40;


                        graphics.DrawString("DOC.3 ", bFont, PdfBrushes.Black, new PointF(0, 0));
                        graphics.DrawString("แบบลงทะเบียนกรรมการ " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition));

                        // สร้างตาราง
                        PdfLightTable table = new PdfLightTable();
                        //Create a DataTable.
                        DataTable dataTable = new DataTable();
                        dataTable.Columns.Add("ลำดับ");
                        dataTable.Columns.Add("ชื่อ");
                        dataTable.Columns.Add("โรงเรียน");
                        dataTable.Columns.Add("ตำแหน่ง");
                        dataTable.Columns.Add("เวลามา");
                        dataTable.Columns.Add("ลายเซ็นมา");
                        dataTable.Columns.Add("เวลากลับ");
                        dataTable.Columns.Add("ลายเซ็นกลับ");

                        //ดึงข้อมูลมา
                        var data1 = await _context.referee.Where(x => x.c_id == id && x.status == "1" && activeSettingIds.Contains(x.SettingID)).ToListAsync();
                        data1 = data1.OrderBy(x => roleOrder.ContainsKey(x.role) ? roleOrder[x.role] : int.MaxValue).ToList();

                        // เพิ่มข้อมูลใน DataTable
                        int index = 1;
                        foreach (var item in data1)
                        {
                            dataTable.Rows.Add(index.ToString(), item.name, item.position, item.role, "", "", "", "");
                            index++;
                        }
                        table.DataSource = dataTable;
                        table.Columns[0].Width = 20;
                        table.Columns[1].Width = 120;
                        table.Columns[2].Width = 120;
                        table.Columns[3].Width = 60;
                        PdfCellStyle defStyle = new PdfCellStyle();
                        defStyle.Font = ttFont12;
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
                        table.Columns[2].StringFormat = format11;



                        // วาดตารางในเอกสาร PDF
                        table.Draw(page2, new PointF(0, 130));
                    }

                }
                // เพิ่มหน้าใหม่ในเอกสาร PDF
                PdfPage page3 = document.Pages.Add();
                PdfGraphics graphics3 = page3.Graphics;
                yPosition = 70;
                // 1. กำหนดตัวแปร Path เริ่มต้น

                // ตรวจสอบว่ามีข้อมูลใน DB หรือไม่
                if (datasetting != null && !string.IsNullOrEmpty(datasetting.LogoPath))
                {
                    // ⚡️ แก้ไข: จัดการเครื่องหมาย Slash ให้ถูกต้องตาม OS (Windows/Linux)
                    // เปลี่ยน / เป็น \ (ถ้าเป็น Windows) และลบตัวหน้าสุดออก
                    string cleanPath = datasetting.LogoPath
                        .Replace("/", Path.DirectorySeparatorChar.ToString())
                        .Replace("\\", Path.DirectorySeparatorChar.ToString())
                        .TrimStart(Path.DirectorySeparatorChar);

                    logoPhysicalPath = Path.Combine(_env.WebRootPath, cleanPath);
                }

                // 2. ตรวจสอบไฟล์ (ถ้าไม่มีไฟล์จริง ให้ใช้ Default)
                if (string.IsNullOrEmpty(logoPhysicalPath) || !System.IO.File.Exists(logoPhysicalPath))
                {
                    // ใช้รูปสำรอง (AdminLTE)
                    // ใช้ Path.Combine เพื่อต่อ Path ให้ถูกต้องแน่นอน
                    logoPhysicalPath = Path.Combine(_env.WebRootPath, "dist", "img", "AdminLTELogo.png");
                }
                // 3. เริ่มวาดโลโก้
                if (System.IO.File.Exists(logoPhysicalPath))
                {
                    try
                    {
                        using (FileStream logoStream = new FileStream(logoPhysicalPath, FileMode.Open, FileAccess.Read))
                        {
                            PdfBitmap logoImage = new PdfBitmap(logoStream);

                            float logoWidth = 75;
                            float logoHeight = 75;
                            float logoX = (pageWidth - logoWidth) / 2;
                            float logoY = 0;
                            graphics3.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                        }
                    }
                    catch
                    {
                        // (ซ่อน Error กรณีไฟล์รูปมีปัญหา)
                    }
                }

                string formattedText3 = WrapText(namejob, 100);
                RectangleF bound3_1 = new RectangleF(20, yPosition, page.GetClientSize().Width, page.GetClientSize().Height - 100);
                DrawTextWithWrapping(page3, formattedText3, bFont, PdfBrushes.Black, bound3_1);
                yPosition += 40;
                graphics3.DrawString("DOC.4 ", bFont, PdfBrushes.Black, new PointF(0, 0));
                graphics3.DrawString("แบบบันทึกคะแนน " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition));


                var dataD = await _context.dCompetitionlist.Where(x => x.h_id == id).ToListAsync();
                // สร้าง PdfGrid สำหรับสร้างตาราง
                PdfGrid pdfGrid = new PdfGrid();

                int totalColumns = 4 + dataD.Count;
                // เพิ่มคอลัมน์ทั้งหมด 7 คอลัมน์
                pdfGrid.Columns.Add(totalColumns);

                // เพิ่มแถวหัวตารางแรก
                pdfGrid.Headers.Add(2);
                PdfGridRow mainHeaderRow = pdfGrid.Headers[0];
                PdfGridRow subHeaderRow = pdfGrid.Headers[1];

                // ตั้งค่าเนื้อหาของหัวตารางหลัก
                mainHeaderRow.Cells[0].Value = "ลำดับ";
                mainHeaderRow.Cells[0].RowSpan = 2;
                mainHeaderRow.Cells[1].Value = "โรงเรียน";
                mainHeaderRow.Cells[1].RowSpan = 2;
                mainHeaderRow.Cells[2].Value = "กลุ่ม";
                mainHeaderRow.Cells[2].RowSpan = 2;
                mainHeaderRow.Cells[3].Value = "เกณฑ์";
                mainHeaderRow.Cells[3].ColumnSpan = dataD.Count; // ผสานเซลล์ให้ครอบคลุมจำนวนเกณฑ์
                                                                 // แก้ไขส่วนนี้เพื่อให้คอลัมน์ "รวม" แสดงและผสานเซลล์ถูกต้อง
                mainHeaderRow.Cells[3 + dataD.Count].Value = "รวม";
                mainHeaderRow.Cells[3 + dataD.Count].RowSpan = 2; // ผสานเซลล์แถวหลักและแถวรอง

                pdfGrid.Columns[0].Width = 20;
                pdfGrid.Columns[1].Width = 160;
                pdfGrid.Columns[2].Width = 30;



                // จัดกึ่งกลางหัวตารางหลักทั้งหมด
                for (int i = 0; i < mainHeaderRow.Cells.Count; i++)
                {
                    mainHeaderRow.Cells[i].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                }

                // เพิ่มชื่อย่อยสำหรับคอลัมน์เกณฑ์
                int r = 1;
                for (int i = 3; i < 3 + dataD.Count; i++)
                {
                    // นำค่าจาก dataD (เช่น dr.Name) มาต่อท้าย r ในวงเล็บ
                    var valueFromDataD = dataD[i - 3].scrol; // สมมติว่าต้องการใช้ property 'Name' จาก dataD
                    subHeaderRow.Cells[i].Value = $"{r}\n({valueFromDataD})"; // แสดงค่าที่ต้องการในวงเล็บ
                    subHeaderRow.Cells[i].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                    r++;
                }

                // ปรับแต่งลักษณะเซลล์หัวตารางหลัก
                for (int i = 0; i < 3; i++)
                {
                    mainHeaderRow.Cells[i].Style.Font = ttFont12;
                }
                mainHeaderRow.Cells[3].Style.Font = ttFont12;
                mainHeaderRow.Cells[3 + dataD.Count].Style.Font = bFont;

                // กำหนดให้หัวตารางซ้ำในหน้าถัดไปโดยอัตโนมัติ
                pdfGrid.RepeatHeader = true;

                int x = 1;
                foreach (var dr in data.OrderBy(x => x.School.Name))
                {
                    // เพิ่มแถวข้อมูลตัวอย่าง
                    PdfGridRow dataRow = pdfGrid.Rows.Add();
                    dataRow.Cells[0].Value = x.ToString();
                    dataRow.Cells[1].Value = dr.School.Name;
                    dataRow.Cells[2].Value = dr.School.grouplist.Name;

                    dataRow.Cells[0].Style.Font = ttFont12;
                    dataRow.Cells[1].Style.Font = ttFont12;
                    dataRow.Cells[2].Style.Font = ttFont12;

                    // กำหนดการจัดการจัดกึ่งกลาง
                    dataRow.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle,
                    };
                    dataRow.Cells[2].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                    x += 1;

                }
                //เกณฑ์การแข่งขัน
                PdfGridRow footerRow = pdfGrid.Rows.Add();
                string footerText = "เกณฑ์การตัดสิน\n";

                float currentYPosition = 130f;
                // ดึงข้อมูลจากฐานข้อมูล
                var judges = await _context.referee.Where(x => x.c_id == id && activeSettingIds.Contains(x.SettingID)).ToListAsync();
                judges = judges.OrderBy(x => roleOrder.ContainsKey(x.role) ? roleOrder[x.role] : int.MaxValue).ToList();
                // แทนที่ 'Judges' ด้วยชื่อ Model หรือ Table ที่เหมาะสม
                string chair = judges.FirstOrDefault(x => x.role == "ประธาน")?.name ?? "";
                string secretary = judges.FirstOrDefault(x => x.role == "กรรมการและเลขานุการ")?.name ?? "";
                var committeeNames = judges.Where(x => x.role == "กรรมการ").Select(x => x.name).ToList();
                int countCommitteeNames = data.FirstOrDefault()?.Competitionlist?.director != null
     ? Convert.ToInt32(data.FirstOrDefault().Competitionlist.director)
     : 0;
                float totalTableHeight = 0;


                //  pdfGrid.Draw(page3, new PointF(0, currentYPosition));
                PdfLayoutResult gridResult = pdfGrid.Draw(page3, new PointF(0, currentYPosition));
                PdfPage currentPage = gridResult.Page; // หน้าใหม่ที่ตารางไปอยู่ (อัตโนมัติหากตารางข้ามหน้า)
                float nextPositionY = gridResult.Bounds.Bottom + 10;
                // ตรวจสอบว่าตำแหน่งถัดไปเกินหน้าหรือไม่
                if (nextPositionY >= currentPage.GetClientSize().Height)
                {
                    // เพิ่มหน้าใหม่
                    currentPage = document.Pages.Add();
                    nextPositionY = 20; // เริ่มจากด้านบนของหน้าใหม่
                }
                PdfLayoutFormat format = new PdfLayoutFormat();
                format.Layout = PdfLayoutType.Paginate;
                #region htmlText

                string longtext2 = "<b>เกณฑ์การตัดสิน</b><br/>";
                foreach (var dr in dataD)
                {
                    longtext2 += $"{dr.id}. {dr.name} {dr.scrol} คะแนน<br/>"; // เพิ่ม \n เพื่อขึ้นบรรทัดใหม่
                }


                #endregion

                // วาด HTML Text ในตำแหน่งที่คำนวณได้
                PdfHTMLTextElement richTextElement2 = new PdfHTMLTextElement(longtext2, ttFont16, brush);
                PdfLayoutResult result2 = richTextElement2.Draw(
     currentPage, // ใช้หน้าที่คำนวณได้
     new RectangleF(20, nextPositionY, currentPage.GetClientSize().Width - 40, currentPage.GetClientSize().Height - nextPositionY),
     format
 );

                PdfPage currentPage1 = result2.Page; // หน้าใหม่ที่ตารางไปอยู่ (อัตโนมัติหากตารางข้ามหน้า)
                float nextPositionY1 = result2.Bounds.Bottom + 20;
                // ตรวจสอบว่าตำแหน่งถัดไปเกินหน้าหรือไม่
                if (nextPositionY1 >= currentPage1.GetClientSize().Height)
                {
                    // เพิ่มหน้าใหม่
                    currentPage1 = document.Pages.Add();
                    nextPositionY1 = 20; // เริ่มจากด้านบนของหน้าใหม่
                }
                // ตารางใหม่
                PdfGrid grid = new PdfGrid();

                grid.Columns.Add(3); // สร้าง 3 คอลัมน์

                // กำหนดความกว้างของคอลัมน์
                grid.Columns[0].Width = 170; // คอลัมน์ 1
                grid.Columns[1].Width = 170; // คอลัมน์ 2
                grid.Columns[2].Width = 170; // คอลัมน์ 3

                // แถวที่ 1: ประธาน (ผสานเซลล์ 3 คอลัมน์)
                PdfGridRow row1 = grid.Rows.Add();
                row1.Cells[0].ColumnSpan = 3; // ผสานเซลล์ 3 คอลัมน์
                row1.Cells[0].Value = $"ลงชื่อ............................ ประธาน\n({chair})\nเบอร์โทร....................";
                row1.Cells[0].Style = new PdfGridCellStyle
                {
                    Font = ttFont16,
                    StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle),
                    Borders = new PdfBorders { All = PdfPens.Transparent }   // กำหนดเส้นขอบ
                };



                // แถวที่ 2: กรรมการ 3 คน
                PdfGridRow row2 = grid.Rows.Add();
                for (int i = 0; i < 3; i++)
                {
                    if (i < committeeNames.Count && !string.IsNullOrEmpty(committeeNames[i]))
                    {
                        row2.Cells[i].Value = $"ลงชื่อ............................ กรรมการ\n({committeeNames[i]})\nเบอร์โทร....................";
                    }
                    else
                    {
                        row2.Cells[i].Value = "ลงชื่อ............................ กรรมการ\n(                      )\nเบอร์โทร....................";
                    }
                }
                foreach (PdfGridCell cell in row2.Cells)
                {
                    cell.Style = new PdfGridCellStyle
                    {
                        Font = ttFont16,
                        StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle),
                        Borders = new PdfBorders { All = PdfPens.Transparent }   // กำหนดเส้นขอบ
                    };
                }
                int currentMemberIndex = 3;
                int remainingMembers = committeeNames.Count - 3;
                while (remainingMembers > 0)
                {
                    // เพิ่มแถวใหม่
                    PdfGridRow row = grid.Rows.Add();

                    // วางกรรมการในแถวใหม่
                    for (int i = 0; i < 3; i++)
                    {
                        // ตรวจสอบว่ายังมีคนเหลือให้แสดงไหม
                        if (remainingMembers > 0 && currentMemberIndex < committeeNames.Count)
                        {
                            // ✅ แก้ไขจุดนี้: ใช้ currentMemberIndex แทน 3 + i
                            row.Cells[i].Value = $"ลงชื่อ............................ กรรมการ\n({committeeNames[currentMemberIndex]})\nเบอร์โทร....................";

                            // ขยับไปคนถัดไป
                            currentMemberIndex++;
                            remainingMembers--;
                        }
                        else
                        {
                            // ถ้าไม่มีคนเหลือแล้วในแถวนี้ ให้ใส่ค่าว่างหรือจัดรูปแบบตามต้องการ
                            row.Cells[i].Value = "";
                        }
                    }

                    // ปรับแต่งให้กับแต่ละเซลล์ในแถวใหม่
                    foreach (PdfGridCell cell in row.Cells)
                    {
                        cell.Style = new PdfGridCellStyle
                        {
                            Font = ttFont16,
                            StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle),
                            Borders = new PdfBorders { All = PdfPens.Transparent }   // กำหนดเส้นขอบ
                        };
                    }
                }

                // แถวที่ 3: เลขานุการ (ผสานเซลล์ 3 คอลัมน์)
                PdfGridRow row4 = grid.Rows.Add();
                row4.Cells[0].ColumnSpan = 3; // ผสานเซลล์ 3 คอลัมน์
                row4.Cells[0].Value = $"       ลงชื่อ..................................กรรมการและเลขานุการ\n({secretary})\nเบอร์โทร....................";
                row4.Cells[0].Style = new PdfGridCellStyle
                {
                    Font = ttFont16,
                    StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle),
                    Borders = new PdfBorders { All = PdfPens.Transparent }   // กำหนดเส้นขอบ
                };
                float estimatedGridHeight = grid.Rows.Count * 100;

                // เช็คว่าพื้นที่ที่เหลือในหน้า พอสำหรับตารางไหม?
                float pageHeight = currentPage1.GetClientSize().Height;
                if (nextPositionY1 + estimatedGridHeight > pageHeight)
                {
                    // ถ้าไม่พอ ให้ขึ้นหน้าใหม่ทันที
                    currentPage1 = document.Pages.Add();
                    nextPositionY1 = 20; // รีเซ็ตตำแหน่ง Y สำหรับหน้าใหม่
                }
                // เพิ่มตารางลงในหน้า PDF
                grid.Draw(currentPage1, new PointF(0, nextPositionY1)); // วางตำแหน่งตาราง
                ///ใบให้คะแนนของกรรมการแต่ละคน
                ///
                foreach (var rn in judges)
                {
                    // เพิ่มหน้าใหม่ในเอกสาร PDF
                    PdfPage page4 = document.Pages.Add();
                    PdfGraphics graphics4 = page4.Graphics;
                    yPosition = 70;
                    // 1. กำหนดตัวแปร Path เริ่มต้น
                    // string logoPhysicalPath = ""; 
                    // 3. เริ่มวาดโลโก้
                    if (System.IO.File.Exists(logoPhysicalPath))
                    {
                        try
                        {
                            using (FileStream logoStream = new FileStream(logoPhysicalPath, FileMode.Open, FileAccess.Read))
                            {
                                PdfBitmap logoImage = new PdfBitmap(logoStream);

                                float logoWidth = 75;
                                float logoHeight = 75;
                                float logoX = (pageWidth - logoWidth) / 2;
                                float logoY = 0;
                                graphics4.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                            }
                        }
                        catch
                        {
                            // (ซ่อน Error กรณีไฟล์รูปมีปัญหา)
                        }
                    }
                    DrawTextWithWrapping(page4, formattedText3, bFont, PdfBrushes.Black, bound3_1);
                    yPosition += 40;
                    graphics4.DrawString("DOC.4.1 ", bFont, PdfBrushes.Black, new PointF(0, 0));
                    graphics4.DrawString("แบบบันทึกคะแนน " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition));
                    // สร้าง PdfGrid สำหรับสร้างตาราง
                    PdfGrid pdfGrid4_1 = new PdfGrid();
                    int totalColumns4_1 = 4 + dataD.Count;
                    // เพิ่มคอลัมน์ทั้งหมด 7 คอลัมน์
                    pdfGrid4_1.Columns.Add(totalColumns);

                    // เพิ่มแถวหัวตารางแรก
                    pdfGrid4_1.Headers.Add(2);
                    PdfGridRow mainHeaderRow4_1 = pdfGrid4_1.Headers[0];
                    PdfGridRow subHeaderRow4_1 = pdfGrid4_1.Headers[1];

                    // ตั้งค่าเนื้อหาของหัวตารางหลัก
                    mainHeaderRow4_1.Cells[0].Value = "ลำดับ";
                    mainHeaderRow4_1.Cells[0].RowSpan = 2;
                    mainHeaderRow4_1.Cells[1].Value = "โรงเรียน";
                    mainHeaderRow4_1.Cells[1].RowSpan = 2;
                    mainHeaderRow4_1.Cells[2].Value = "กลุ่ม";
                    mainHeaderRow4_1.Cells[2].RowSpan = 2;
                    mainHeaderRow4_1.Cells[3].Value = "เกณฑ์";
                    mainHeaderRow4_1.Cells[3].ColumnSpan = dataD.Count; // ผสานเซลล์ให้ครอบคลุมจำนวนเกณฑ์
                                                                        // แก้ไขส่วนนี้เพื่อให้คอลัมน์ "รวม" แสดงและผสานเซลล์ถูกต้อง
                    mainHeaderRow4_1.Cells[3 + dataD.Count].Value = "รวม";
                    mainHeaderRow4_1.Cells[3 + dataD.Count].RowSpan = 2; // ผสานเซลล์แถวหลักและแถวรอง

                    pdfGrid4_1.Columns[0].Width = 20;
                    pdfGrid4_1.Columns[1].Width = 160;
                    pdfGrid4_1.Columns[2].Width = 30;



                    // จัดกึ่งกลางหัวตารางหลักทั้งหมด
                    for (int i = 0; i < mainHeaderRow4_1.Cells.Count; i++)
                    {
                        mainHeaderRow4_1.Cells[i].StringFormat = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle
                        };
                    }

                    // เพิ่มชื่อย่อยสำหรับคอลัมน์เกณฑ์
                    int r4_1 = 1;
                    for (int i = 3; i < 3 + dataD.Count; i++)
                    {
                        // นำค่าจาก dataD (เช่น dr.Name) มาต่อท้าย r ในวงเล็บ
                        var valueFromDataD = dataD[i - 3].scrol; // สมมติว่าต้องการใช้ property 'Name' จาก dataD
                        subHeaderRow4_1.Cells[i].Value = $"{r4_1}\n({valueFromDataD})"; // แสดงค่าที่ต้องการในวงเล็บ
                        subHeaderRow4_1.Cells[i].StringFormat = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle
                        };
                        r4_1++;
                    }

                    // ปรับแต่งลักษณะเซลล์หัวตารางหลัก
                    for (int i = 0; i < 3; i++)
                    {
                        mainHeaderRow4_1.Cells[i].Style.Font = ttFont12;
                    }
                    mainHeaderRow4_1.Cells[3].Style.Font = ttFont12;
                    mainHeaderRow4_1.Cells[3 + dataD.Count].Style.Font = bFont;

                    // กำหนดให้หัวตารางซ้ำในหน้าถัดไปโดยอัตโนมัติ
                    pdfGrid4_1.RepeatHeader = true;

                    int x4_1 = 1;
                    foreach (var dr in data.OrderBy(x => x.School.Name))
                    {
                        // เพิ่มแถวข้อมูลตัวอย่าง
                        PdfGridRow dataRow = pdfGrid4_1.Rows.Add();
                        dataRow.Cells[0].Value = x4_1.ToString();
                        dataRow.Cells[1].Value = dr.School.Name;
                        dataRow.Cells[2].Value = dr.School.grouplist.Name;

                        dataRow.Cells[0].Style.Font = ttFont12;
                        dataRow.Cells[1].Style.Font = ttFont12;
                        dataRow.Cells[2].Style.Font = ttFont12;

                        // กำหนดการจัดการจัดกึ่งกลาง
                        dataRow.Cells[0].StringFormat = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle,
                        };
                        dataRow.Cells[2].StringFormat = new PdfStringFormat
                        {
                            Alignment = PdfTextAlignment.Center,
                            LineAlignment = PdfVerticalAlignment.Middle
                        };
                        x4_1 += 1;

                    }
                    PdfGridRow footerRow4_1 = pdfGrid4_1.Rows.Add();
                    string footerText4_1 = "เกณฑ์การตัดสิน\n";
                    foreach (var dr in dataD)
                    {
                        footerText4_1 += $"{dr.id}. {dr.name} {dr.scrol} คะแนน\n"; // เพิ่ม \n เพื่อขึ้นบรรทัดใหม่
                    }

                    // ตั้งค่าเนื้อหาของส่วนท้าย เช่น "รวมทั้งหมด"
                    footerRow4_1.Cells[0].Value = footerText4_1;
                    footerRow4_1.Cells[0].ColumnSpan = totalColumns; // ผสานเซลล์ให้ครอบคลุมคอลัมน์ลำดับ, โรงเรียน, กลุ่ม, และเกณฑ์ทั้งหมด
                    footerRow4_1.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Left,  // จัดข้อความให้อยู่ขวา
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                    footerRow4_1.Cells[0].Style.Font = ttFont12; // กำหนดฟอนต์

                    float currentYPosition4_1 = 130f;

                    float totalTableHeight4_1 = 0;
                    foreach (PdfGridRow row in pdfGrid4_1.Rows)
                    {
                        totalTableHeight += row.Height;
                    }


                    // แถวที่ 1: ประธาน
                    PdfGridRow chairRow4_1 = pdfGrid4_1.Rows.Add();
                    chairRow4_1.Cells[0].ColumnSpan = totalColumns;
                    chairRow4_1.Cells[0].Value = $"ลงชื่อ............................{rn.role}\n   ({rn.name})\nเบอร์โทร.........................";
                    chairRow4_1.Cells[0].Style.Font = ttFont12;
                    chairRow4_1.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                    // ลบเส้นขอบของแถวกรรมการ
                    foreach (PdfGridCell cell in chairRow4_1.Cells)
                    {
                        cell.Style.Borders.All = PdfPens.Transparent;
                    }
                    // วาดตารางในหน้าปัจจุบัน
                    pdfGrid4_1.Draw(page4, new PointF(0, currentYPosition));
                }

                // เพิ่มหน้าใหม่ในเอกสาร PDF
                PdfPage page5 = document.Pages.Add();
                PdfGraphics graphics5 = page5.Graphics;
                yPosition = 50;
                // 3. เริ่มวาดโลโก้
                if (System.IO.File.Exists(logoPhysicalPath))
                {
                    try
                    {
                        using (FileStream logoStream = new FileStream(logoPhysicalPath, FileMode.Open, FileAccess.Read))
                        {
                            PdfBitmap logoImage = new PdfBitmap(logoStream);

                            float logoWidth = 75;
                            float logoHeight = 75;
                            float logoX = (pageWidth - logoWidth) / 2;
                            float logoY = 0;
                            graphics5.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
                        }
                    }
                    catch
                    {
                        // (ซ่อน Error กรณีไฟล์รูปมีปัญหา)
                    }
                }

                graphics5.DrawString("DOC.5 ", bFont, PdfBrushes.Black, new PointF(0, yPosition));
                graphics5.DrawString("เอกสารการเปลี่ยนตัว แก้ไข เพิ่ม ชื่อ-สกุลของนักเรียน/ครูผู้สอน/กรรมการ ", bFont, PdfBrushes.Black, new PointF(0, yPosition += bFont.Height));
                graphics5.DrawString("รายการ " + datacom.Name, bFont, PdfBrushes.Black, new PointF(0, yPosition += bFont.Height));

                // สร้างตารางใหม่
                PdfGrid pdfGrid2 = new PdfGrid();

                // กำหนดจำนวนคอลัมน์
                pdfGrid2.Columns.Add(6);
                pdfGrid2.Columns[0].Width = 20f;
                pdfGrid2.Columns[1].Width = 100f;

                // เพิ่มหัวตาราง
                PdfGridRow headerRow2 = pdfGrid2.Headers.Add(1)[0];
                headerRow2.Cells[0].Value = "ที่";
                headerRow2.Cells[1].Value = "รายการ";
                headerRow2.Cells[2].Value = "ชื่อ-สกุลเดิม";
                headerRow2.Cells[3].Value = "ชื่อ-สกุลใหม่";
                headerRow2.Cells[4].Value = "โรงเรียนและสังกัด";
                headerRow2.Cells[5].Value = "เกี่ยวข้องกับการแข่งขัน";

                // กำหนดรูปแบบของหัวตาราง
                foreach (PdfGridCell headerCell in headerRow2.Cells)
                {
                    headerCell.Style.Font = bFont1;
                    headerCell.StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                }

                // เพิ่มข้อมูลในตาราง
                float checkboxSize = 10f;
                float initialX = 25f;
                float initialX1 = 420f;
                float initialY = 130f; // ตำแหน่งเริ่มต้นของแถวแรก
                float yOffset = initialY; // ใช้ค่านี้ในการเพิ่มตำแหน่ง
                int rowIndex = 1;

                // ทำการเพิ่มข้อมูลให้ครบ
                for (int rr = 0; rr < 10; rr++) // loop over your data
                {
                    PdfGridRow row = pdfGrid2.Rows.Add();

                    // กำหนดค่าให้กับแต่ละเซลล์ในแถว
                    row.Cells[0].Value = rowIndex.ToString();
                    row.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle
                    };
                    row.Cells[0].Style.Font = ttFont16;

                    row.Cells[1].Value = "       เปลี่ยนตัว\n       แก้ไข\n       เพิ่มรายชื่อ";
                    row.Cells[1].Style.Font = ttFont16;

                    yOffset = initialY + (rr * 60f); // เปลี่ยน 20f เป็น 40f สำหรับเว้นระยะให้พอดี

                    // วาด checkbox
                    graphics5.DrawRectangle(PdfPens.Black, initialX, yOffset, checkboxSize, checkboxSize);
                    graphics5.DrawRectangle(PdfPens.Black, initialX, yOffset + 20f, checkboxSize, checkboxSize);
                    graphics5.DrawRectangle(PdfPens.Black, initialX, yOffset + 40f, checkboxSize, checkboxSize);

                    graphics5.DrawRectangle(PdfPens.Black, initialX1, yOffset, checkboxSize, checkboxSize);
                    graphics5.DrawRectangle(PdfPens.Black, initialX1, yOffset + 20f, checkboxSize, checkboxSize);
                    graphics5.DrawRectangle(PdfPens.Black, initialX1, yOffset + 40f, checkboxSize, checkboxSize);

                    // เพิ่มค่าช่องอื่นๆ
                    row.Cells[2].Value = ""; // ชื่อ-สกุลเดิม
                    row.Cells[3].Value = ""; // ชื่อ-สกุลใหม่
                    row.Cells[4].Value = ""; // โรงเรียนและสังกัด
                    row.Cells[5].Value = "       นักเรียน\n       ครู\n       กรรมการ"; // เกี่ยวข้องกับการแข่งขัน
                    row.Cells[5].Style.Font = ttFont16;

                    rowIndex++;
                }

                // กำหนดให้ตารางแสดงบน PDF
                pdfGrid2.Draw(page5, new PointF(0, yPosition += bFont.Height + 5));

                // ปรับตำแหน่ง y ให้เหมาะสมหลังจากวาดตาราง
                currentYPosition += pdfGrid2.Rows.Count * 20; // ปรับค่า 20 ตามความสูงของแต่ละแถว

                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", "เอกสารการแข่งขัน.pdf");
                }
            }
        }
        public async Task<IActionResult> printreferee(int id, int type)
        {
            System.Globalization.CultureInfo thaiCulture = new System.Globalization.CultureInfo("th-TH");
            var datasetting = await _context.setupsystem.Where(x => x.status == "1").FirstOrDefaultAsync();
            var activeSettingIds = await _context.setupsystem
    .Where(s => s.status == "1")
    .Select(s => s.id)
    .ToListAsync();
            using (PdfDocument document = new PdfDocument())
            {
                // เพิ่มหน้ากระดาษใหม่
                // PdfPage page = document.Pages.Add();
                var pathFont = _env.WebRootPath + "/Font/THSarabun.ttf";
                Stream fontStream = new FileStream(System.IO.Path.Combine(pathFont), FileMode.Open, FileAccess.Read);
                PdfTrueTypeFont BFont = new PdfTrueTypeFont(fontStream, 22, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Bold);
                PdfTrueTypeFont bFont1 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Bold);
                PdfTrueTypeFont BFontbFont2 = new PdfTrueTypeFont(fontStream, 10, PdfFontStyle.Regular);
                PdfTrueTypeFont bFont8 = new PdfTrueTypeFont(fontStream, 8, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont = new PdfTrueTypeFont(fontStream, 14, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont12 = new PdfTrueTypeFont(fontStream, 12, PdfFontStyle.Regular);
                PdfTrueTypeFont ttFont16 = new PdfTrueTypeFont(fontStream, 16, PdfFontStyle.Regular);
                PdfSolidBrush brush = new PdfSolidBrush(Color.Black);
                // เพิ่มหน้าใหม่ในเอกสาร
                PdfPage page = document.Pages.Add();
                float pageWidth = page.GetClientSize().Width;
                int i = 1;
                string text1 = $"เอกสารแนบท้าย {i} รายชื่อคณะกรรมการดําเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน\n";
                text1 += $"{datasetting.name}";


                string pathpic = _env.WebRootPath + "/PDFgen/t1.jpg";
                FileStream imageStream = new FileStream(pathpic, FileMode.Open, FileAccess.Read);
                PdfBitmap image1 = new PdfBitmap(imageStream);

                float width = 114;
                float height = 114;
                float x = (pageWidth - width) / 2;
                float y = 0;
                page.Graphics.DrawImage(image1, x, y, width, height);

                // กำหนดลำดับการเรียงโดยใช้ Dictionary
                var roleOrder = new Dictionary<string, int>
{
    { "ประธาน", 1 },
    { "รองประธาน", 2 },
    { "กรรมการ", 3 },
    { "กรรมการและเลขานุการ", 4 }
};


                // ข้อความ
                string text2 = "ประกาศสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา";
                string text3 = "เรื่อง แต่งตั้งคณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวด";
                string text4 = $"{datasetting.name}";
                string text5 = "--------------------------------------------------------";

                // คำนวณความกว้างของข้อความและจัดกึ่งกลาง
                float xCenter1 = (page.GetClientSize().Width - bFont.MeasureString(text2).Width) / 2;
                float xCenter2 = (page.GetClientSize().Width - bFont.MeasureString(text3).Width) / 2;
                float xCenter3 = (page.GetClientSize().Width - bFont.MeasureString(text4).Width) / 2;
                float xCenter4 = (page.GetClientSize().Width - bFont.MeasureString(text5).Width) / 2;

                float yPosition = 120; // เริ่มต้นที่แนวตั้ง
                float lineSpacing = 15; // ระยะห่างระหว่างบรรทัด

                // วาดข้อความลงในหน้า
                page.Graphics.DrawString(text2, bFont, brush, new PointF(xCenter1, yPosition));
                yPosition += lineSpacing;
                page.Graphics.DrawString(text3, bFont, brush, new PointF(xCenter2, yPosition));
                yPosition += lineSpacing;
                page.Graphics.DrawString(text4, bFont, brush, new PointF(xCenter3, yPosition));
                yPosition += lineSpacing;
                page.Graphics.DrawString(text5, bFont, brush, new PointF(xCenter4, yPosition));
                yPosition += lineSpacing;


                #region htmlText

                string longtext = "               เพื่อให้การจัดการแข่งขันงานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓<br/>" +
                                  "ปีการศึกษา ๒๕๖๘ เป็นไปด้วยความเรียบร้อย ตรงตามวัตถุประสงค์ของการจัดงานศิลปหัตถกรรมนักเรียน<br/>" +
                                  "เป็นเวทีให้นักเรียนได้แสดงออก ถึงความรู้ ความสามารถของตนเอง นักเรียนได้รับการพัฒนา ทักษะทางด้าน<br/>" +
                                  "วิชาการ วิชาชีพ ดนตรี นาฏศิลป์ ศิลปะ เห็นคุณค่าและเกิดความภาคภูมิใจในความเป็นไทย รักและหวงแหน <br/> " +
                                  "ในมรดกทางวัฒนธรรมของไทย รวมทั้งการใช้กิจกรรมเป็นสื่อเพื่อการพัฒนาคุณธรรม จริยธรรม เสริมสร้างวิถี <br/>" +
                                  "ประชาธิปไตย และคุณลักษณะอันพึงประสงค์ตามหลักสูตร การสร้างภูมิคุ้มกันภัยจากยาเสพติด และแสดงให้<br/>" +
                                  "เห็นถึงผลสำเร็จของการจัดการศึกษาของครูผู้สอน การเผยแพร่ผลงานด้านการจัดการศึกษาสู่สาธารณชน ตาม <br/>" +
                                  "ประกาศสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา เรื่อง การจัดการแข่งขันงานศิลปหัตถกรรม<br/>" +
                                  "นักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๒ ปีการศึกษา ๒๕๖๗ และประกาศสำนักงานเขตพื้นที่การศึกษา<br/>" +
                                  "มัธยมศึกษานครราชสีมา เรื่อง สถานที่การจัดการแข่งขันงานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา<br/>" +
                                  "ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ <br/>" +
                                  "               อาศัยอำนาจตามความใน มาตรา ๓๙ แห่งพระราชบัญญัติระเบียบบริหารราชการ<br/>" +
                                  "กระทรวงศึกษาธิการ พ.ศ. ๒๕๔๖ ประกอบ มาตรา ๒๔ แห่งพระราชบัญญัติระเบียบข้าราชการครูและ<br/>" +
                                  "บุคลากรทางการศึกษา พ.ศ. ๒๕๔๗ และมติที่ประชุมร่วม กําหนดการจัดการแข่งขันงานศิลปหัตถกรรม<br/>" +
                                  "นักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ เมื่อวันที่ ๑๒ พฤศจิกายน ๒๕๖๘ ระหว่าง<br/>" +
                                  "สำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา สำนักงานเขตพื้นที่การศึกษาประถมศึกษานครราชสีมา<br/>" +
                                  "เขต ๓, เขต ๕ และ เขต ๗ องค์การบริหารส่วนจังหวัดนครราชสีมา สำนักงานเทศบาลนครราชสีมา<br/>" +
                                  "สถานศึกษาเอกชนจังหวัดนครราชสีมา สำนักงานศึกษาธิการจังหวัดนครราชสีมา โรงเรียนสุรวิวัฒน์<br/>" +
                                  "มหาวิทยาลัยเทคโนโลยีสุรนารี และโรงเรียนสาธิต มหาวิทยาลัยราชภัฏนครราชสีมา ฝ่ายมัธยม<br/>" +
                                  "               จึงขอประกาศแต่งตั้งคณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวด<br/>" +
                                  "แข่งขันงานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ระหว่างวันที่<br/>" +
                                  "๑๐ - ๑๒ ธันวาคม ๒๕๖๘ รายละเอียดปรากฏตามเอกสารแนบท้ายประกาศนี้<br/>" +
                                  "               ๑. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้ภาษาไทย โรงเรียนปากช่อง (รายละเอียดตามเอกสารแนบท้าย ๑)<br/>" +
                                  "               ๒. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้คณิตศาสตร์ โรงเรียนราชสีมาวิทยาลัย (รายละเอียดตามเอกสารแนบท้าย ๒)<br/>" +
                                  "               ๓. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้วิทยาศาสตร์และเทคโนโลยี โรงเรียนบุญวัฒนา (รายละเอียดตามเอกสารแนบท้าย ๓)<br/>" +
                                  "               ๔. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้สังคมศึกษา ศาสนาและวัฒนธรรม โรงเรียนบุญเหลือวิทยานุสรณ์<br/>" +
                                  "(รายละเอียดตามเอกสารแนบท้าย ๔)<br/>" +
                                   "               ๕. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้สุขศึกษาและพลศึกษา โรงเรียนราชสีมาวิทยาลัย (รายละเอียดตามเอกสารแนบท้าย ๕)<br/>" +
                                   "               ๖. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้ศิลปะ โรงเรียนพิมายวิทยา (รายละเอียดตามเอกสารแนบท้าย ๖)<br/>" +
                                  "               ๗. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้การงานอาชีพ โรงเรียนโชคชัยสามัคคี (รายละเอียดตามเอกสารแนบท้าย ๗)<br/>" +
                                  "               ๘. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์พัฒนาวิชาการ<br/>" +
                                  "กลุ่มสาระการเรียนรู้ภาษาต่างประเทศ โรงเรียนสุรนารีวิทยา (รายละเอียดตามเอกสารแนบท้าย ๘)<br/>" +
                                  "               ๙. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์กิจกรรมพัฒนา<br/>" +
                                  "ผู้เรียน โรงเรียนอุบลรัตนราชกัญญา ราชวิทยาลัยนครราชสีมา (รายละเอียดตามเอกสารแนบท้าย ๙)<br/>" +
                                  "               ๑๐. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์เทคโนโลยี<br/>" +
                                  "สารสนเทศและการสื่อสาร โรงเรียนสุรธรรมพิทักษ์ (รายละเอียดตามเอกสารแนบท้าย ๑๐)<br/>" +
                                  "               ๑๐. คณะกรรมการดำเนินงานและคณะกรรมการตัดสินกิจกรรมการประกวดแข่งขัน<br/>" +
                                  "งานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ศูนย์การจัดการศึกษา<br/>" +
                                  "เรียนรวม โรงเรียนบุญเหลือวิทยานุสรณ์ (รายละเอียดตามเอกสารแนบท้าย ๑๑ )<br/>" +
                                   "                    ทั้งนี้ ตั้งแต่บัดนี้เป็นต้นไป<br/>" +
                                    "                              ประกาศ ณ วันที่ ๒๕ พฤศจิกายน พ.ศ. ๒๕๖๘<br/>" +
                                     "                         <br/>" +
                                      "                         <br/>" +
                                       "                         <br/>" +
                                       "                                                       ว่าร้อยเอก ดร.ทิณกรณ์ ภูโทถ้ำ<br/>" +
                                        "                             ผู้อำนวยการสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา<br/>" +
                                  ""
                                  ;

                #endregion


                //Rendering HtmlText
                PdfHTMLTextElement richTextElement = new PdfHTMLTextElement(longtext, ttFont16, brush);

                // Formatting Layout
                PdfLayoutFormat format = new PdfLayoutFormat();
                format.Layout = PdfLayoutType.Paginate; // ใช้ Paginate เพื่อให้ข้อความที่เกินหน้าเลื่อนไปหน้าถัดไป
                                                        //  format.Layout = PdfLayoutType.OnePage;

                //Drawing htmlString
                richTextElement.Draw(page, new RectangleF(20, yPosition, page.GetClientSize().Width, page.GetClientSize().Height), format);
                var datarefereeAll = await _context.referee.Where(x => x.status == "1" && activeSettingIds.Contains(x.SettingID)).ToListAsync();

                ////หน้าที่ 2
                ///
                int s = 1;
                int[] s_id = { 12, 13, 14, 16, 17, 19, 21, 22, 23, 15, 24, 32 };
                string[] s_name = { "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้ภาษาไทย", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้คณิตศาสตร์", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้วิทยาศาสตร์และเทคโนโลยี", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้สังคมศึกษาศาสนาและวัฒนธรรม", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้สุขศึกษาและพลศึกษา", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้ศิลปะ", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้การงานอาชีพ", "ศูนย์พัฒนาวิชาการกลุ่มสาระการเรียนรู้ภาษาต่างประเทศ", "ศูนย์กิจกรรมพัฒนาผู้เรียน", "ศูนย์เทคโนโลยีสารสนเทศและการสื่อสาร", "ศูนย์การจัดการศึกษาเรียนรวม", "ศูนย์พัฒนาวิชาการแนะแนว" };
                string[] school = { "โรงเรียนปากช่อง", "โรงเรียนราชสีมาวิทยาลัย", "โรงเรียนบุญวัฒนา", "โรงเรียนบุญเหลือวิทยานุสรณ์", "โรงเรียนราชสีมาวิทยาลัย", "โรงเรียนพิมายวิทยา", "โรงเรียนโชคชัยสามัคคี", "โรงเรียนสุรนารีวิทยา", "โรงเรียนอุบลรัตนราชกัญญา ราชวิทยาลัยนครราชสีมา", "โรงเรียนสุรธรรมพิทักษ์", "โรงเรียนบุญเหลือวิทยานุสรณ์", "โรงเรียนบุญวัฒนา" };
                System.Globalization.CultureInfo thaiCulture1 = new System.Globalization.CultureInfo("th-TH");
                // ลูปผ่านค่าใน s_name
                foreach (var name in s_name)
                {
                    string sArabic = s.ToString();
                    string sThai = ToThaiDigits(sArabic);
                    // เพิ่มหน้าใหม่
                    PdfPage page2 = document.Pages.Add();
                    string text2_1 = $"เอกสารแนบท้าย {sThai}";
                    string text2_2 = $"การแข่งขันงานศิลปหัถกรรมนักเรียนมัธยมศึกษา {datasetting.ProvinceName} ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘";
                    string text2_3 = $"{name}";
                    string text2_4 = $"สนามแข่งขัน ณ {school[s - 1]}";
                    string text2_5 = $"ระหว่างวันที่ ๑๐ - ๑๒ ธันวาคม พ.ศ. ๒๕๖๘";
                    string text2_6 = $"***********************************************************************************************************************";
                    float textWidth = ttFont16.MeasureString(text2_1).Width;
                    float xCenter2_1 = (page.GetClientSize().Width - bFont.MeasureString(text2_1).Width) / 2;
                    float xCenter2_2 = (page.GetClientSize().Width - bFont.MeasureString(text2_2).Width) / 2;
                    float xCenter2_3 = (page.GetClientSize().Width - bFont.MeasureString(text2_3).Width) / 2;
                    float xCenter2_4 = (page.GetClientSize().Width - bFont.MeasureString(text2_4).Width) / 2;
                    float xCenter2_5 = (page.GetClientSize().Width - bFont.MeasureString(text2_5).Width) / 2;
                    float xCenter2_6 = (page.GetClientSize().Width - bFont.MeasureString(text2_6).Width) / 2;
                    float xPosition2_1 = (page2.Size.Width - textWidth) / 2;
                    float yPosition2 = 0;
                    page2.Graphics.DrawString(text2_1, bFont, brush, new PointF(xCenter2_1, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString(text2_2, ttFont16, brush, new PointF(xCenter2_2, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString(text2_3, bFont, brush, new PointF(xCenter2_3, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString(text2_4, ttFont16, brush, new PointF(xCenter2_4, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString(text2_5, ttFont16, brush, new PointF(xCenter2_5, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString(text2_6, ttFont16, brush, new PointF(xCenter2_6, yPosition2));
                    yPosition2 += 15;
                    page2.Graphics.DrawString($"คณะกรรมการ{name} ประกอบด้วย", ttFont16, brush, new PointF(10, yPosition2));
                    //yPosition2 += 15;
                    //page2.Graphics.DrawString($"ประกอบด้วย", ttFont16, brush, new PointF(10, yPosition2));
                    yPosition2 += 15;
                    var data = await _context.groupreferee.Where(x => x.c_id == s_id[s - 1] && activeSettingIds.Contains(x.SettingID)).ToListAsync();
                    PdfGrid pdfGrid = new PdfGrid();

                    PdfPen borderPen;

                    // เพิ่มคอลัมน์ 4 คอลัมน์
                    pdfGrid.Columns.Add(4);
                    pdfGrid.Columns[0].Width = 10;
                    pdfGrid.Columns[3].Width = 105;



                    var datagroupreferee = await _context.groupreferee.Where(x => x.c_id == s_id[s - 1] && x.type == "2" && activeSettingIds.Contains(x.SettingID)).ToListAsync();

                    int no = 1;
                    foreach (var dr in datagroupreferee)
                    {
                        // เพิ่มแถวแรก
                        PdfGridRow headerRow = pdfGrid.Rows.Add();


                        // กำหนดข้อความในเซลล์แถวแรก
                        headerRow.Cells[0].Value = $"{no}.{dr.name}";
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

                        var datareferee = datarefereeAll.Where(x => x.m_id == s_id[s - 1] && x.g_id == dr.id && activeSettingIds.Contains(x.SettingID))
                        .OrderBy(x => roleOrder.ContainsKey(x.role) ? roleOrder[x.role] : int.MaxValue)
                        .ToList();
                        // เพิ่มแถวข้อมูลปกติ
                        int no1 = 1;
                        foreach (var ddr in datareferee)
                        {
                            PdfGridRow row = pdfGrid.Rows.Add();
                            row.Cells[0].Value = $"";
                            row.Cells[0].Style.Font = ttFont16;
                            row.Cells[1].Value = $"{no}.{no1} {ddr.name}";
                            row.Cells[1].Style.Font = ttFont16;
                            row.Cells[2].Value = $"{ddr.position}";
                            row.Cells[2].Style.Font = ttFont16;
                            row.Cells[3].Value = $"{ddr.role}";
                            row.Cells[3].Style.Font = ttFont16;
                            no1 += 1;
                            foreach (PdfGridCell cell in row.Cells)
                            {
                                cell.Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส
                            }
                        }
                        no += 1;
                    }
                    List<int> m_idList = new List<int> { };//12, 13, 14, 16, 17, 19, 21, 22, 23, 15, 24 
                    if (s_id[s - 1] == 12)
                    {
                        m_idList.Add(12); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 13)
                    {
                        m_idList.Add(13); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 14)
                    {
                        m_idList.Add(14); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 16)
                    {
                        m_idList.Add(1); // ใช้ Add() แทน Add =
                        m_idList.Add(16); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 17)
                    {
                        m_idList.Add(4); // ใช้ Add() แทน Add =
                        m_idList.Add(17); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 19)
                    {
                        m_idList.Add(5); // ใช้ Add() แทน Add =
                        m_idList.Add(6); // ใช้ Add() แทน Add =
                        m_idList.Add(7); // ใช้ Add() แทน Add =
                        m_idList.Add(19); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 21)
                    {
                        m_idList.Add(3); // ใช้ Add() แทน Add =
                        //m_idList.Add(21); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 22)
                    {
                        m_idList.Add(22); // ใช้ Add() แทน Add =
                        //m_idList.Add(21); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 23)
                    {
                        m_idList.Add(8); // ใช้ Add() แทน Add =
                        m_idList.Add(23); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 15)
                    {
                        m_idList.Add(2); // ใช้ Add() แทน Add =
                        m_idList.Add(15); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 24)
                    {
                        //m_idList.Add(2); // ใช้ Add() แทน Add =
                        m_idList.Add(24); // ใช้ Add() แทน Add =
                    }
                    else if (s_id[s - 1] == 32)
                    {
                        //m_idList.Add(2); // ใช้ Add() แทน Add =
                        m_idList.Add(32); // ใช้ Add() แทน Add =
                    }
                    var dataCompetitionlist = await _context.Competitionlist.Where(x => x.status == "1" && x.c_id.HasValue && m_idList.Contains(x.c_id.Value)).ToListAsync();
                    foreach (var dr in dataCompetitionlist)
                    {
                        // เพิ่มแถวแรก
                        PdfGridRow headerRow = pdfGrid.Rows.Add();


                        // กำหนดข้อความในเซลล์แถวแรก
                        headerRow.Cells[0].Value = $"{no}.{dr.Name}";
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

                        List<int> m_idList1 = new List<int>();
                        if (s_id[s - 1] == 15)
                        {
                            m_idList1.Add(2);
                            m_idList1.Add(15);
                        }
                        else if (s_id[s - 1] == 21)
                        {
                            m_idList1.Add(3);
                            m_idList1.Add(21);
                        }
                        else if (s_id[s - 1] == 17)
                        {
                            m_idList1.Add(4);
                            m_idList1.Add(17);
                        }
                        else if (s_id[s - 1] == 16)
                        {
                            m_idList1.Add(1);
                            m_idList1.Add(16);
                        }
                        else if (s_id[s - 1] == 19)
                        {
                            m_idList1.Add(5);
                            m_idList1.Add(6);
                            m_idList1.Add(7);
                            m_idList1.Add(19);
                        }

                        else
                        {
                            m_idList1.Add(s_id[s - 1]);
                        }
                        var datareferee = datarefereeAll.Where(x => x.c_id == dr.Id).ToList();
                        datareferee = datareferee.Where(x => m_idList1.Contains(x.m_id))
                        .OrderBy(x => roleOrder.ContainsKey(x.role) ? roleOrder[x.role] : int.MaxValue)
                        .ToList();

                        // เพิ่มแถวข้อมูลปกติ
                        int no1 = 1;
                        foreach (var ddr in datareferee)
                        {
                            PdfGridRow row = pdfGrid.Rows.Add();
                            row.Cells[0].Value = $"";
                            row.Cells[0].Style.Font = ttFont16;
                            row.Cells[1].Value = $"{no}.{no1} {ddr.name}";
                            row.Cells[1].Style.Font = ttFont16;
                            row.Cells[2].Value = $"{ddr.position}";
                            row.Cells[2].Style.Font = ttFont16;
                            row.Cells[3].Value = $"{ddr.role}";
                            row.Cells[3].Style.Font = ttFont16;
                            no1 += 1;
                            foreach (PdfGridCell cell in row.Cells)
                            {
                                cell.Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส
                            }
                        }
                        no += 1;
                    }
                    pdfGrid.Draw(page2, new PointF(10, yPosition2 + 15));

                    s += 1;
                }
                ///คำสั้งศูนย์
                ///
                PdfPage page3 = document.Pages.Add();
                float pageWidth3 = page3.GetClientSize().Width;

                page3.Graphics.DrawImage(image1, x, y, width, height);




                // ข้อความ
                string text3_1 = "คำสั่งสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา";
                string text3_2 = "ที่           / ๒๕๖๘";
                string text3_3 = "เรื่อง แต่งตั้งคณะกรรมการคัดเลือก กรรมการตัดสินการแข่งขันงานศิลปหัตถกรรมนักเรียน";
                string text3_4 = "ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘";

                float xCenter3_1 = (page.GetClientSize().Width - bFont.MeasureString(text3_1).Width) / 2;
                float xCenter3_2 = (page.GetClientSize().Width - bFont.MeasureString(text3_2).Width) / 2;
                float xCenter3_3 = (page.GetClientSize().Width - bFont.MeasureString(text3_3).Width) / 2;
                float xCenter3_4 = (page.GetClientSize().Width - bFont.MeasureString(text3_4).Width) / 2;
                yPosition = 120; // เริ่มต้นที่แนวตั้ง


                // วาดข้อความลงในหน้า
                page3.Graphics.DrawString(text3_1, bFont, brush, new PointF(xCenter3_1, yPosition));
                yPosition += lineSpacing;
                page3.Graphics.DrawString(text3_2, bFont, brush, new PointF(xCenter3_2, yPosition));
                yPosition += lineSpacing;
                page3.Graphics.DrawString(text3_3, bFont, brush, new PointF(xCenter3_3, yPosition));
                yPosition += lineSpacing;
                page3.Graphics.DrawString(text3_4, bFont, brush, new PointF(xCenter3_4, yPosition));
                yPosition += lineSpacing;
                page3.Graphics.DrawString(text5, bFont, brush, new PointF(xCenter4, yPosition));
                yPosition += lineSpacing;
                #region htmlText

                string longtext1 = "               ด้วยสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา ได้แล็งเห็นถึงความสำคัญของการจัดงาน<br/>" +
                                  "ศิลปหัตถกรรมนักเรียน เพื่อให้สอดคล้องตามเจตนารมณ์ของพระราชบัญญัติการศึกษาแห่งชาติ และสานต่อเจตนารมณ์<br/>" +
                                  "ของการจัดการศึกษาที่มุ่งให้การจัดงานศิลปหัตถกรรมนักเรียน เป็นเวทีให้นักเรียนได้แสดงออกถึงความรู้<br/>" +
                                  "ความสามารถของตนเองอย่างอิสระและสร้างสรรค์ ใช้เวลาว่างให้เกิดประโยชน์ นักเรียนได้รับการพัฒนาทักษะทางด้าน <br/> " +
                                  "วิชาการ วิชาชีพ ดนตรี นาฏศิลป์ ศิลปะ เห็นคุณค่าและเกิดความภาคภูมิใจในความเป็นไทย รักและหวงแหนในมรดก<br/>" +
                                  "ทางวัฒนธรรมของไทย รวมทั้งการใช้กิจกรรมเป็นสื่อเพื่อการพัฒนาคุณธรรม จริยธรรม เสริมสร้างวิธีประชาธิปไตย และ<br/>" +
                                  "คุณลักษณะอันพึงประสงค์ตามหลักสูตร และการสร้างภูมิคุ้มกันภัยจากยาเสพติด และแสดงให้เห็นถึงผลสำเร็จของการ <br/>" +
                                  "จัดการศึกษาของครูผู้สอน การเผยแแพร่ผลงานด้านการจัดการศึกษาสู่สาธารณชน จึงกำหนดการจัดการแข่งขันงาน<br/>" +
                                  "ศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ระหว่างวันที่ ๑๐ - ๑๒ ธันวาคม ๒๕๖๘<br/>" +
                                  " ณ สนามแข่งขัน โรงเรียนในสังกัดสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา เพื่อให้การดำเนินงาน<br/>" +
                                  "เป็นไปตามวัตถุประสงค์ และประสิทธิภาพ โดยอาศัยอำนาจตามความในมาตรา ๓๗ แห่งพระราชบัญญัติบริหาร<br/>" +
                                  "ราชการกระทรวงศึกษาธิการ พ.ศ.๒๕๔๖ และมาตรา ๒๔ แห่งพระราชบัญญัติระเบียบราชการครูและบุคลากร<br/>" +
                                  "ทางการศึกษา พ.ศ.๒๕๔๗ และที่แก้ไขเพิ่มเติม จึงแต่งตั้งคณะกรรมการพิจารณาและคัดเลือก กรรมการตัดสินการ<br/>" +
                                  "แข่งขันงานศิลปหัตถกรรมนักเรียน ระดับเขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ดังนี้<br/>"
                                  ;

                #endregion
                //Rendering HtmlText
                PdfHTMLTextElement richTextElement1 = new PdfHTMLTextElement(longtext1, ttFont16, brush);
                PdfLayoutResult result = richTextElement1.Draw(page3, new RectangleF(20, yPosition, page3.GetClientSize().Width, page3.GetClientSize().Height), format);

                // ตรวจสอบว่าเนื้อหา HTML ถูกวาดสำเร็จ
                if (result == null || result.Bounds.Height == 0)
                {
                    throw new Exception("Error: Unable to draw HTML content.");
                }

                // คำนวณตำแหน่งเริ่มต้นของตาราง
                float tableStartY = result.Bounds.Bottom + 10;

                // สร้างตาราง
                PdfGrid pdfGrid5 = new PdfGrid();
                // เพิ่มคอลัมน์ 4 คอลัมน์
                pdfGrid5.Columns.Add(4);
                pdfGrid5.Columns[0].Width = 10;
                pdfGrid5.Columns[3].Width = 105;
                PdfGridRow headerRow5 = pdfGrid5.Rows.Add();
                // กำหนดข้อความในเซลล์แถวแรก
                headerRow5.Cells[0].Value = $"คณะกรรมการอำนวยการ";
                // รวมเซลล์ 4 คอลัมน์
                headerRow5.Cells[0].ColumnSpan = 4;

                // จัดข้อความให้อยู่ตรงกลาง
                headerRow5.Cells[0].StringFormat = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Left, // จัดกึ่งกลางแนวนอน
                    LineAlignment = PdfVerticalAlignment.Middle // จัดกึ่งกลางแนวตั้ง
                };
                headerRow5.Cells[0].Style.Font = bFont;
                headerRow5.Cells[0].Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส

                var datareferee31 = datarefereeAll.Where(x => x.m_id == 31).ToList();
                int no5 = 1;
                foreach (var ddr in datareferee31)
                {
                    PdfGridRow row5 = pdfGrid5.Rows.Add();
                    row5.Cells[0].Value = $"";
                    row5.Cells[0].Style.Font = ttFont16;
                    row5.Cells[1].Value = $"{no5}.{ddr.name}";
                    row5.Cells[1].Style.Font = ttFont16;
                    row5.Cells[2].Value = $"{ddr.position}";
                    row5.Cells[2].Style.Font = ttFont16;
                    row5.Cells[3].Value = $"{ddr.role}";
                    row5.Cells[3].Style.Font = ttFont16;
                    foreach (PdfGridCell cell in row5.Cells)
                    {
                        cell.Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส
                    }
                    no5 += 1;
                }
                PdfGridRow headerRow6 = pdfGrid5.Rows.Add();
                // กำหนดข้อความในเซลล์แถวแรก
                headerRow6.Cells[0].Value = $"มีหน้าที่ ให้คำปรึกษา และอำนวยการในการดำเนินงาน ให้เป็นไปด้วยความเรียบร้อย สำเร็จลุล่วงไปด้วยดี";
                // รวมเซลล์ 4 คอลัมน์
                headerRow6.Cells[0].ColumnSpan = 4;

                // จัดข้อความให้อยู่ตรงกลาง
                headerRow6.Cells[0].StringFormat = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Left, // จัดกึ่งกลางแนวนอน
                    LineAlignment = PdfVerticalAlignment.Middle // จัดกึ่งกลางแนวตั้ง
                };
                headerRow6.Cells[0].Style.Font = ttFont16;
                headerRow6.Cells[0].Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส




                int z = 1;
                foreach (var name in s_name)
                {
                    var groupreferee = await _context.groupreferee.Where(x => x.c_id == s_id[z - 1] && x.type == "1").FirstOrDefaultAsync();
                    PdfGridRow headerRow7 = pdfGrid5.Rows.Add();
                    // กำหนดข้อความในเซลล์แถวแรก
                    headerRow7.Cells[0].Value = $"{z}.{name}";
                    // รวมเซลล์ 4 คอลัมน์
                    headerRow7.Cells[0].ColumnSpan = 4;

                    // จัดข้อความให้อยู่ตรงกลาง
                    headerRow7.Cells[0].StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Left, // จัดกึ่งกลางแนวนอน
                        LineAlignment = PdfVerticalAlignment.Middle // จัดกึ่งกลางแนวตั้ง
                    };
                    headerRow7.Cells[0].Style.Font = bFont;
                    headerRow7.Cells[0].Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส

                    var datareferee = datarefereeAll.Where(x => x.m_id == s_id[z - 1] && x.g_id == (groupreferee?.id ?? -1))
                     .OrderBy(x => roleOrder.ContainsKey(x.role) ? roleOrder[x.role] : int.MaxValue)
                    .ToList();
                    // เพิ่มแถวข้อมูลปกติ
                    int z_1 = 1;
                    foreach (var ddr in datareferee)
                    {
                        PdfGridRow row = pdfGrid5.Rows.Add();
                        row.Cells[0].Value = $"";
                        row.Cells[0].Style.Font = ttFont16;
                        row.Cells[1].Value = $"{z}.{z_1} {ddr.name}";
                        row.Cells[1].Style.Font = ttFont16;
                        row.Cells[2].Value = $"{ddr.position}";
                        row.Cells[2].Style.Font = ttFont16;
                        row.Cells[3].Value = $"{ddr.role}";
                        row.Cells[3].Style.Font = ttFont16;
                        z_1 += 1;
                        foreach (PdfGridCell cell in row.Cells)
                        {
                            cell.Style.Borders.All = PdfPens.Transparent; // เส้นขอบแต่ละเซลล์โปร่งใส
                        }
                    }


                    z += 1;
                }
                PdfLayoutResult gridResult = pdfGrid5.Draw(page3, new PointF(20, tableStartY));
                PdfPage currentPage = gridResult.Page; // หน้าใหม่ที่ตารางไปอยู่ (อัตโนมัติหากตารางข้ามหน้า)
                float nextPositionY = gridResult.Bounds.Bottom + 10;
                // ตรวจสอบว่าตำแหน่งถัดไปเกินหน้าหรือไม่
                if (nextPositionY >= currentPage.GetClientSize().Height)
                {
                    // เพิ่มหน้าใหม่
                    currentPage = document.Pages.Add();
                    nextPositionY = 20; // เริ่มจากด้านบนของหน้าใหม่
                }

                #region htmlText

                string longtext2 = "มีหน้าที่       ๑) พิจารณาและคัดเลือกคณะกรรมการ ตัดสินการแข่งขันงานศิลปหัตถกรรมนักเรียน ระดับ<br/>" +
                                  "เขตพื้นที่การศึกษา ครั้งที่ ๗๓ ปีการศึกษา ๒๕๖๘ ตามที่กลุ่มสาระการเรียนรู้ หรือกิจกรรมที่รับผิดชอบจัดการ<br/>" +
                                  "แข่งขัน จากผู้สมัครผ่านโปรแกรมการแข่งขัน หรือ เสนอรายชื่อผู้ที่มีความรู้ ความสามารถ ตามความเหมาะสม<br/>" +
                                  "                ๒) พิจารณาวินิจฉัยชี้ขาดเรื่องอุทธรณ์<br/> " +
                                  "               ๓) พิจารณาวินิจฉัยชี้ขาดเรื่องร้องเรียน<br/>" +
                                  "                ๔) รายงานผลการดำเนินงานต่อผู้อำนวยการสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา<br/>" +
                                  "               ให้คณะกรรมการที่ได้รับแต่งตั้ง ปฏิบัติหน้าที่ที่ได้รับมอบหมายให้เป็นไปด้วยความเรียบร้อย มีประสิทธิภาพ<br/>" +
                                  "บังเกิดผลดีแก่ทางราชการ หากมีปัญหา อุปสรรคในการดำเนินงาน ให้รายงานต่อผู้อำนวยการสำนักงานเขตพื้นที่<br/>" +
                                  "การศึกษามัธยมศึกษานครราชสีมา ทราบ เพื่อพิจารณาดำเนินการต่อ<br/>" +
                                  "               ทั้งนี้ ตั้งแต่บัดนี้เป็นต้นไป<br/>" +
                                  "                              สั่ง ณ วันที่ ๑ ธันวาคม พ.ศ.๒๕๖๘<br/>" +
                                  "                         <br/>" +
                                      "                         <br/>" +
                                       "                         <br/>" +
                                       "                                                       ว่าที่ร้อยเอก ดร.ทิณกรณ์ ภูโทถ้ำ<br/>" +
                                        "                             ผู้อำนวยการสำนักงานเขตพื้นที่การศึกษามัธยมศึกษานครราชสีมา<br/>" +
                                  ""
                                  ;

                #endregion

                // วาด HTML Text ในตำแหน่งที่คำนวณได้
                PdfHTMLTextElement richTextElement2 = new PdfHTMLTextElement(longtext2, ttFont16, brush);
                PdfLayoutResult result2 = richTextElement2.Draw(
     currentPage, // ใช้หน้าที่คำนวณได้
     new RectangleF(20, nextPositionY, currentPage.GetClientSize().Width - 40, currentPage.GetClientSize().Height - nextPositionY),
     format
 );
                // บันทึกเอกสารไปยัง MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    document.Save(stream);
                    document.Close();

                    // ส่งไฟล์ PDF กลับไปยังผู้ใช้
                    stream.Position = 0; // Reset the stream position
                    return File(stream.ToArray(), "application/pdf", "คำสั่งคณะกรรมการแต่ละศูนย์.pdf");
                }
            }
        }
        // ฟังก์ชันสำหรับแปลงเลขเลขอารบิก -> เลขไทย
        public string ToThaiDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("0", "๐")
                .Replace("1", "๑")
                .Replace("2", "๒")
                .Replace("3", "๓")
                .Replace("4", "๔")
                .Replace("5", "๕")
                .Replace("6", "๖")
                .Replace("7", "๗")
                .Replace("8", "๘")
                .Replace("9", "๙");
        }
        public void DrawTextWithWrapping(PdfPage page, string text, PdfFont font, PdfBrush brush, RectangleF bounds)
        {
            PdfTextElement textElement = new PdfTextElement(text, font, brush)
            {
                StringFormat = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Top
                }
            };

            // วาดข้อความบนหน้า PDF และตัดบรรทัดอัตโนมัติในพื้นที่ที่กำหนด
            textElement.Draw(page, bounds);
        }
        public void DrawTextWithWrappingLeft(PdfPage page, string text, PdfFont font, PdfBrush brush, RectangleF bounds)
        {
            PdfTextElement textElement = new PdfTextElement(text, font, brush)
            {
                StringFormat = new PdfStringFormat
                {
                    Alignment = PdfTextAlignment.Left,
                    LineAlignment = PdfVerticalAlignment.Top
                }
            };

            // วาดข้อความบนหน้า PDF และตัดบรรทัดอัตโนมัติในพื้นที่ที่กำหนด
            textElement.Draw(page, bounds);
        }
        public static string WrapText(string text, int maxLineLength)
        {
            StringBuilder wrappedText = new StringBuilder();
            int lineLength = 0;

            foreach (var word in text.Split(' '))
            {
                // ตรวจสอบว่าความยาวของข้อความในบรรทัดนั้นเกินที่กำหนดหรือไม่
                if (lineLength + word.Length > maxLineLength)
                {
                    wrappedText.AppendLine(); // ขึ้นบรรทัดใหม่
                    lineLength = 0;
                }

                wrappedText.Append(word + " ");
                lineLength += word.Length + 1;
            }

            return wrappedText.ToString();
        }
        private List<string> WrapText1(string text, PdfFont font, float maxWidth, PdfGraphics graphics)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');

            string currentLine = "";
            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float textWidth = font.MeasureString(testLine).Width;
                if (textWidth > maxWidth)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }
        // ฟังก์ชันช่วยจัดแถวข้อความ
        private void DrawRow(PdfGraphics graphics, PdfTrueTypeFont font, float columnWidth, float y, string[] texts)
        {
            float x = 0;
            foreach (var text in texts)
            {
                // วาดข้อความในแต่ละคอลัมน์
                graphics.DrawString(text, font, PdfBrushes.Black, new RectangleF(x, y, columnWidth, 60), new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle));
                x += columnWidth; // เลื่อนไปยังคอลัมน์ถัดไป
            }
        }
        public string GetCompetitionDetails(racedetails datadd, CultureInfo thaiCulture)
        {
            //var datadd = racedetails.(x => x.c_id == c_id).FirstOrDefault();
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
    }
}
