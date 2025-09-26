using System.Net;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class registerdirector
    {
        [Key]
        public int id { get; set; }
        [Display(Name = "ชื่อ-สกุล")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลชื่อ-สกุล")]
        public string name { get; set; }
        [Display(Name = "ประสบการณ์ประสบการณ์การ")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลประสบการณ์ประสบการณ์การ")]
        public string experience { get; set; }
        [Display(Name = "ตำแหน่ง/โรงเรียน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลตำแหน่ง/โรงเรียน")]
        public string position { get; set; }
        [Display(Name = "เบอร์โทร")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลเบอร์โทร")]
        public string tel { get; set; }
        public int g_id { get; set; }
        public int c_id { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? status{ get; set; }
        public DateTime? lastupdate { get; set; }

         [NotMapped]
        [Required(ErrorMessage = "กรุณาเลือกรูปภาพก่อนบันทึกข้อมูล")]
        public IFormFile ProfileImage { get; set; }



        [ForeignKey("c_id")]
        public virtual Competitionlist? Competitionlist { get; set; }

    }
}