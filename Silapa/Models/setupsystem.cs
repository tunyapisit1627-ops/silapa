using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace Silapa.Models
{
    public class setupsystem
    {
        [Key]
        public int id { get; set; }
        [Display(Name = "ชื่องาน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลงาน")]
        public string name { get; set; }
        [Display(Name = "ชื่อหน่วยงาน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลหน่วยงาน")]
        public string nameagency { get; set; }
        [Display(Name = "ครั้งที่")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลครั้งที่")]
        public string time { get; set; }
        [Display(Name = "ปี")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลปี")]
        public string yaer { get; set; }

        [Display(Name = "วันที่เริ่มลงทะเบียน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันเริ่มลงทะเบียน")]
        public string startregisterdate { get; set; }
        [Display(Name = "วันที่สุดท้ายลงทะเบียน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันวันสุดท้ายลงทะเบียน")]
        public string endgisterdate { get; set; }
        [Display(Name = "วันที่เริ่มแก้ไขข้อมูล")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันที่เริ่มแก้ไขข้อมูล")]
        public string startedit { get; set; }
        [Display(Name = "วันที่สิ้นสุดแก้ไขข้อมูล")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันที่สิ้นสุดแก้ไขข้อมูล")]
        public string endedit { get; set; }
        [Display(Name = "วันที่แข่งขัน")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันที่แข่งขัน")]
        public string racedate { get; set; }
        [Display(Name = "วันที่เริ่มดาวน์โหลดเกียรติบัตร")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลวันที่เริ่มดาวน์โหลดเกียรติบัตร")]
        public string certificatedate { get; set; }

        [Display(Name = "ผู้ประสานงาน")]
        [Required(ErrorMessage = "ผู้ประสานงาน")]
        public string? Coordinator { get; set; }
        [Display(Name = "รหัส token line")]
        public string? token { get; set; }
        public string? LogoPath { get; set; }
        public string? certificate { get; set; }
        public string? cardstudents { get; set; }
        public string? cardteacher { get; set; }
        public string? carddirector { get; set; }
        public string? cardreferee { get; set; }
        public string u_id { get; set; }
        public DateTime? lastupdate { get; set; }

        // ----------------------------------------------------
        // ⬇️ ส่วนที่ต้องเพิ่มใหม่สำหรับ Hero & About Section
        // ----------------------------------------------------

        [Display(Name = "คำโปรย (Slogan) ภาษาไทย")]
        public string? SloganThai { get; set; } // "ศาสตร์ศิลป์ ถิ่นโคราช..."

        [Display(Name = "คำโปรย (Slogan) ภาษาอังกฤษ")]
        public string? SloganEnglish { get; set; } // "The 73rd Excellent..."

        [Display(Name = "ลิงก์วิดีโอ (Video Path)")]
        public string? HeroVideoPath { get; set; } // "vdo/vdo.mp4"

        [Display(Name = "หัวข้อ 'เกี่ยวกับ' (About Heading)")]
        public string? AboutHeading { get; set; } // "ที่หนึ่งของความคิดสร้างสรรค์..."

        [Display(Name = "เนื้อหา 'เกี่ยวกับ' ย่อหน้า 1")]
        [DataType(DataType.MultilineText)] // ทำให้กล่อง Text ใน CMS ใหญ่ขึ้น
        public string? AboutText1 { get; set; } // "เราเชื่อว่าการสร้างสรรค์ที่ดี..."

        [Display(Name = "เนื้อหา 'เกี่ยวกับ' ย่อหน้า 2")]
        [DataType(DataType.MultilineText)]
        public string? AboutText2 { get; set; } // "เราพร้อมนำเสนอสุดยอดผลงาน..."

        [Display(Name = "จังหวัดเจ้าภาพ")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลจังหวัดเจ้าภาพ")]
        public string ProvinceName { get; set; }

        [Display(Name = "ไฟล์ประกาศ (PDF)")]
        public string? DeclarationFilePath { get; set; }


        [NotMapped]
        [JsonIgnore]
        public IFormFile? Logo { get; set; }
        [NotMapped]
        [JsonIgnore]
        public IFormFile? Certificate { get; set; }
        [NotMapped]
        [JsonIgnore]
        public IFormFile? CardStudents { get; set; }
        [NotMapped]
        [JsonIgnore]
        public IFormFile? CardTeacher { get; set; }
        [NotMapped]
        [JsonIgnore]
        public IFormFile? CardDirector { get; set; }
        [NotMapped]
        [JsonIgnore]
        public IFormFile? CardReferee { get; set; }

        [NotMapped]
        [JsonIgnore]
        public IFormFile? DeclarationFile { get; set; }
        public string status { get; set; }


        public virtual ICollection<TimelineItem> TimelineEvents { get; set; }
    }
}