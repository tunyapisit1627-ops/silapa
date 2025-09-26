namespace Silapa.Models
{
    public class ResultGroupViewModel
    {
        public string Code { get; set; } // รหัสกลุ่ม
        public string Name { get; set; } // ชื่อกลุ่ม
        public List<ResultViewModel> Results { get; set; } // รายการผล
    }

    public class ResultViewModel
    {
        public int Order { get; set; } // ลำดับ
        public string Status { get; set; } // สถานะ (ประกาศผล)
        public string Description { get; set; } // รายละเอียด
    }
}