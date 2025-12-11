using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class NewsImage
    {
        [Key]
        public int ImageId { get; set; }

        [Display(Name = "ชื่อไฟล์")]
        public string FileName { get; set; }

        [Display(Name = "URL รูปภาพ")]
        public string ImageUrl { get; set; }

        // Foreign Key เพื่อเชื่อมโยงไปยังข่าวสารหลัก
        public int NewsId { get; set; }

        [ForeignKey("NewsId")]
        public news News { get; set; } // Navigation Property
    }
}