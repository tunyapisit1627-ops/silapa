using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    // (ไฟล์: SchoolReportViewModel.cs)
    public class SchoolReportViewModel
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; }
        public string? AffiliationName { get; set; } // สังกัด
        public string? GroupName { get; set; }       // กลุ่ม

        [Display(Name = "จำนวนทีม")]
        public int TeamCount { get; set; }

        [Display(Name = "จำนวนนักเรียน")]
        public int StudentCount { get; set; }

        [Display(Name = "จำนวนครู")]
        public int TeacherCount { get; set; }
    }
}