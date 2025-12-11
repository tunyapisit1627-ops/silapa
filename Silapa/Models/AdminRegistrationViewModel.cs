namespace Silapa.Models
{
    public class AdminRegistrationViewModel
    {
        // 1. สำหรับ Dashboard (ยอดรวม)
        public int TotalCompetitions { get; set; } // รายการแข่งขันทั้งหมด
        public int TotalTeams { get; set; }        // ทีมสมัครทั้งหมด
        public int TotalStudents { get; set; }     // นักเรียนทั้งหมด
        public int TotalTeachers { get; set; }     // ครูทั้งหมด

        // 2. สำหรับ Dashboard (แยกตามหมวดหมู่)
        public List<CategoryStatViewModel> CategoryStats { get; set; } = new List<CategoryStatViewModel>();

        // 3. สำหรับตารางรายชื่อ (ข้อมูลเดิมของคุณ)
        public List<RegisterViewModel2> Registrations { get; set; } = new List<RegisterViewModel2>();
    }

    public class CategoryStatViewModel
    {
        public string CategoryName { get; set; }
        public int CompetitionCount { get; set; } // จำนวนรายการแข่งขันในหมวดนี้
        public int TeamCount { get; set; }        // จำนวนทีมที่สมัครในหมวดนี้
        public List<CompetitionStatViewModel> Competitions { get; set; } = new List<CompetitionStatViewModel>();
    }
    // ⬇️ (เพิ่ม) คลาสใหม่สำหรับเก็บสถิติรายรายการ
    public class CompetitionStatViewModel
    {
        public string CompetitionName { get; set; }
        public int TeamCount { get; set; }
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
    }
}