using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class criterion
    {
        [Key]
        public int id { get; set; }
        public int g_id { get; set; }
        public string name { get; set;}
        public string? file { get; set;}
        public string? status { get; set; }
        [Display(Name = "รหัสงาน (Setting ID)")]
        public int SettingID { get; set; }
        
        // ----------------------------------------------------
        // ⬇️ 4. เพิ่ม Property นี้สำหรับรับไฟล์
        // ----------------------------------------------------
        [NotMapped]
        [Display(Name = "อัปโหลดไฟล์ PDF ใหม่ (ไม่บังคับ)")]
        public IFormFile? PdfFile { get; set; }
    }
}