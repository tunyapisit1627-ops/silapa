namespace Silapa.Models
{
    public class contacts
    {
        public int id { get; set; }
        public string title { get; set; }
        public string name { get; set; }
        public string position { get; set; }
        public string Location { get; set; }
        public string tel { get; set; }
        public string? ImageUrl { get; set; }
        public string? u_id { get; set; }
        public string status { get; set; } = "1"; // กำหนดค่าเริ่มต้นไปเลย
        public DateTime? lastupdate { get; set; } // ทำให้เป็น nullable
        public int DisplayOrder { get; set; } // สำหรับเก็บลำดับการแสดงผล
    }
}