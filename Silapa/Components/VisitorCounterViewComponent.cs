using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Silapa.Models; // 👈 (เปลี่ยนเป็น Namespace Model ของคุณ)
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Silapa.Components // (เปลี่ยนเป็น Namespace โปรเจกต์ของคุณ)
{
    public class VisitorCounterViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<VisitorCounterViewComponent> _logger;

        // 1. (สำคัญ) Inject DbContext และ IHttpContextAccessor
        public VisitorCounterViewComponent(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<VisitorCounterViewComponent> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            long yearlyVisitsCount = 0; // 👈 (เราจะคืนค่านี้)

            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;
            var week = GetWeekOfYear(today); 

            // 2. (โค้ด Logic เดิมของคุณทั้งหมด)
            if (session.GetString("HasVisited") == null)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // (เราใช้ Logic ที่แก้ไขล่าสุด ที่แยก Add/Update)
                        
                        // --- Daily ---
                        var dailyStats = await _context.VisitorCounts.FirstOrDefaultAsync(vc => vc.VisitDate == today);
                        if (dailyStats == null) {
                            dailyStats = new VisitorCounts { VisitDate = today, Year = year, Month = month, Week = week, VisitCount = 1 };
                            _context.VisitorCounts.Add(dailyStats);
                        } else {
                            dailyStats.VisitCount++;
                            _context.VisitorCounts.Update(dailyStats);
                        }
                        
                        // --- Weekly ---
                        var weeklyStats = await _context.VisitorCounts.FirstOrDefaultAsync(vc => vc.Year == year && vc.Week == week && vc.VisitDate == null);
                        if (weeklyStats == null) {
                            weeklyStats = new VisitorCounts { Year = year, Week = week, VisitCount = 1 };
                            _context.VisitorCounts.Add(weeklyStats);
                        } else {
                            weeklyStats.VisitCount++;
                            _context.VisitorCounts.Update(weeklyStats);
                        }

                        // --- Monthly ---
                        var monthlyStats = await _context.VisitorCounts.FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == month && vc.Week == 0);
                        if (monthlyStats == null) {
                            monthlyStats = new VisitorCounts { Year = year, Month = month, VisitCount = 1 };
                            _context.VisitorCounts.Add(monthlyStats);
                        } else {
                            monthlyStats.VisitCount++;
                            _context.VisitorCounts.Update(monthlyStats);
                        }
                        
                        // --- Yearly ---
                        var yearlyStats = await _context.VisitorCounts.FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == 0 && vc.Week == 0);
                        if (yearlyStats == null) {
                            yearlyStats = new VisitorCounts { Year = year, VisitCount = 1 };
                            _context.VisitorCounts.Add(yearlyStats);
                        } else {
                            yearlyStats.VisitCount++;
                            _context.VisitorCounts.Update(yearlyStats);
                        }
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        yearlyVisitsCount = yearlyStats.VisitCount; // 👈 (เก็บค่าที่อัปเดตแล้ว)
                        session.SetString("HasVisited", "true");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "เกิดข้อผิดพลาดขณะนับสถิติผู้เข้าชม");
                        var stats = await _context.VisitorCounts.AsNoTracking().FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == 0 && vc.Week == 0);
                        yearlyVisitsCount = stats?.VisitCount ?? 0;
                    }
                }
            }
            else
            {
                // (ถ้าเคยเข้าแล้ว: ดึงยอดมาโชว์เฉยๆ)
                var stats = await _context.VisitorCounts.AsNoTracking().FirstOrDefaultAsync(vc => vc.Year == year && vc.Month == 0 && vc.Week == 0);
                yearlyVisitsCount = stats?.VisitCount ?? 0;
            }
            
            // 3. (สำคัญ) คืนค่า "ยอด" กลับไปให้ View
            return View(yearlyVisitsCount);
        }

        // (Helper Method เดิมของคุณ)
        private int GetWeekOfYear(DateTime time)
        {
            // (โค้ด GetWeekOfYear ของคุณ...)
            // (...ตัวอย่าง...)
             System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CurrentCulture;
             int weekNum = ci.Calendar.GetWeekOfYear(time, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
             return weekNum;
        }
    }
}