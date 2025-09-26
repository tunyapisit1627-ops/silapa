using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using iText.Layout.Element;

namespace Silapa.Models
{
    public class racedetails
    {
        [Key]
        public int id { get; set; }
        public int c_id { get; set; }
        [Display(Name = "สถานที่")]
        [Required(ErrorMessage = "กรุณาเลือกสถานที่")]
        public int r_id { get; set; }

        [Display(Name = "อาคาร")]
        [Required(ErrorMessage = "กรุณากรอกอาคาร")]
        public string? building { get; set; }
        [Display(Name = "ห้อง")]
        [Required(ErrorMessage = "กรุณากรอกข้อมูลห้อง")]
        public string? room { get; set; }
        [Display(Name = "เวลาเริ่มแข่งขัน")]
        [Required(ErrorMessage = "กรุณากรอกเวลาเริ่มแข่งขัน")]

        public string time { get; set; }

        [Display(Name = "รายละเอียดการแข่งขัน")]
        [Required(ErrorMessage = "กรุณากรอกรายละเอียดการแข่งขัน")]
        public string daterace { get; set; }
        [Required]
        public string details { get; set; }
        public string? u_id { get; set; }
        public string? status { get; set; }
        public DateTime? lastupdate { get; set; }

        [ForeignKey("c_id")]
        public virtual Competitionlist? Competitionlist { get; set; }
        [ForeignKey("r_id")]
        public virtual Racelocation? Racelocation { get; set; }

    }
}