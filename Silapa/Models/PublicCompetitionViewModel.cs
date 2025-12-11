namespace Silapa.Models
{
    public class PublicCompetitionViewModel
    {
        /// <summary>
        /// ID ของรายการแข่งขัน (จาก Competitionlist.Id)
        /// ใช้สำหรับสร้าง ID ของ element ใน HTML เช่น data-target="#schools-1"
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ชื่อรายการแข่งขัน (จาก Competitionlist.Name)
        /// </summary>
        public string? CompetitionName { get; set; }

        /// <summary>
        /// ชื่อหมวดหมู่ (จาก Competitionlist.Category.Name)
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// จำนวนทีม/คนที่รับสมัครสูงสุด (จาก Competitionlist.student)
        /// </summary>
        public int MaxTeams { get; set; }

        /// <summary>
        /// รายชื่อโรงเรียนที่ลงทะเบียนแล้ว (ดึงมาจาก Registration -> School.SchoolName)
        /// </summary>
        /// 
        // --- 🚨 เพิ่ม 3 Properties นี้เข้าไป ---
        /// <summary>
        /// ประเภทการแข่งขัน (เช่น "เดี่ยว", "ทีม")
        /// </summary>
        public string? CompetitionType { get; set; }

        /// <summary>
        /// จำนวนนักเรียนสูงสุดที่กำหนด (จาก Competitionlist.student)
        /// </summary>
        public int StudentLimit { get; set; }

        /// <summary>
        /// จำนวนครูสูงสุดที่กำหนด (จาก Competitionlist.teacher)
        /// </summary>
        public int TeacherLimit { get; set; }
        // --- 🚨 เพิ่ม 2 Properties นี้เข้าไป ---
        /// <summary>
        /// จำนวนนักเรียนที่ลงทะเบียนแล้วทั้งหมดในรายการนี้
        /// </summary>
        public int RegisteredStudentCount { get; set; }

        /// <summary>
        /// จำนวนครูที่ลงทะเบียนแล้วทั้งหมดในรายการนี้
        /// </summary>
        public int RegisteredTeacherCount { get; set; }
        public List<string> RegisteredSchools { get; set; } = new List<string>();
        public List<SchoolRegistrationDetailViewModel> SchoolRegistrationDetails { get; set; } = new List<SchoolRegistrationDetailViewModel>();
    }
    public class SchoolRegistrationDetailViewModel
    {
        public string? SchoolName { get; set; }
        public int RegisteredStudentCount { get; set; }
        public int RegisteredTeacherCount { get; set; }
    }
}