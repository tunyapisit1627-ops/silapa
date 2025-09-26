using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class filelist
    {
        [Key]
        public int id { get; set; }
        public string? name { get; set; }
        public string? fileurl { get; set; }
        public string? u_id { get; set; }
        public string? status { get; set; }
        public DateTime? lastupdate { get; set; }

        [NotMapped] // ถ้าไม่ต้องการเก็บไฟล์ในฐานข้อมูล
        public IFormFile? File { get; set; }
    }
}