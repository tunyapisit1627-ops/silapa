using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class news
    {
        [Key]
        public int id { get; set; }
        [Display(Name = "หัวข้อข่าว")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลหัวข้อข่าว")]
        public string titlename { get; set; }
        [Display(Name = "รายละเอียดข่าว")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลรายละเอียดข่าว")]
        public string details { get; set; }
        public string? ImageUrl { get; set; }
        public string? u_id { get; set; }
        public string? m_id { get; set; }
        public string? status { get; set; }
        public DateTime? lastupdate { get; set; }

        // ฟิลด์สำหรับรับไฟล์ที่อัปโหลด
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}