
namespace Silapa.Models
{
    public class AdminScheduleViewModel
    {
        public int Id { get; set; } // racedetails.id
        public string CompetitionName { get; set; } // ชื่อรายการ
        public string CategoryName { get; set; } // หมวดหมู่
        public string DateRange { get; set; } // วันที่แข่งขัน
        public string Time { get; set; } // เวลา
        public string LocationName { get; set; } // สนามแข่งขัน
        public string Building { get; set; } // อาคาร
        public string Room { get; set; } // ห้อง
        public string Status { get; set; } // สถานะ
        public int CompetitionId { get; set; } // c_id (สำหรับลิงก์แก้ไข)
    }
}
