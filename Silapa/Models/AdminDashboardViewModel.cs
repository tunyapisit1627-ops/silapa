namespace Silapa.Models
{
    public class AdminDashboardViewModel
    {
        // --- 1. สรุปยอดรวม ---
        public int TotalUsers { get; set; }
        public int TotalSchools { get; set; }
        public int TotalRegistrations { get; set; } // (เช่น ยอดทีมที่สมัคร)
        public int TotalCompetitions { get; set; } // (เช่น รายการแข่งขันทั้งหมด)

        // --- ⬇️ 2. (เพิ่ม) ยอดนักเรียน, ครู, กรรมการ ---
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalJudges { get; set; }

        // --- 3. สรุปสถิติผู้เข้าชม (จากตาราง VisitorCounts) ---
        public int DailyVisits { get; set; }
        public int MonthlyVisits { get; set; }
        public int YearlyVisits { get; set; }

        // --- 3. (Optional) รายการสำหรับ Quick View ---
        // public List<ApplicationUser> LatestUsers { get; set; }
        // public List<school> LatestSchools { get; set; }
        public List<CategoryRegistrationCount> CategoryStats { get; set; }
    }
    public class CategoryRegistrationCount
    {
        public string CategoryName { get; set; }
        public int TeamCount { get; set; }
    }
}