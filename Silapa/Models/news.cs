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
        // 🚨 NEW: คุณสมบัติที่ตรงกับ HTML
        [Display(Name = "หมวดหมู่")]
        public string Category { get; set; } // ใช้ใน <span class="news-category">

        [Display(Name = "ข้อความ Badge")]
        public string BadgeText { get; set; } // ใช้ใน <div class="news-badge">

        [Display(Name = "สี Badge (เช่น breaking, new)")]
        public string BadgeClass { get; set; } // ใช้กำหนดสี เช่น breaking/new

        [Display(Name = "สีพื้นหลัง (Gradient CSS)")]
        public string BackgroundGradient { get; set; } // ใช้ใน style="..."

        // NEW: การจัดการรูปภาพ
        // 1. ImageUrl เดิม (สำหรับภาพ Cover หลัก)
        public string? CoverImageUrl { get; set; }

        // 2. Navigation Property สำหรับรูปภาพหลายภาพ
        public ICollection<NewsImage> GalleryImages { get; set; } = new List<NewsImage>();

        // 3. ฟิลด์สำหรับรับไฟล์ที่อัปโหลด *หลายไฟล์*
        [NotMapped]
        [Display(Name = "อัปโหลดไฟล์รูปภาพ (หลายไฟล์ได้)")]
        public List<IFormFile>? ImageFiles { get; set; } // เปลี่ยนเป็น List<IFormFile>
        public string? u_id { get; set; }
        public string? m_id { get; set; }
        public string? status { get; set; }
        public DateTime? lastupdate { get; set; }
        [Display(Name = "ปักหมุด")]
        public bool IsPinned { get; set; } = false;

        // ฟิลด์สำหรับรับไฟล์ที่อัปโหลด
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        [NotMapped]
        public IFormFile[] GalleryFiles { get; set; }
    }
}