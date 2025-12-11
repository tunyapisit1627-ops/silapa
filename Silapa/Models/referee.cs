using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace Silapa.Models
{
    public class referee
    {
        [Key]
        public int id { get; set; }
         public int SettingID{ get; set; }
        [Display(Name = "ชื่อ-สกุล")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลชื่อ-สกุล")]
        public string name { get; set; }
        [Display(Name = "บทบาทหน้าที่")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลบทบาทหน้าที่")]
        public string role{ get; set; }
        [Display(Name = "ตำแหน่ง")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลตำแหน่ง")]
        public string position { get; set; }    
         public string? ImageUrl{ get; set; }
        public string? u_id { get; set; }
        [Display(Name = "หมวดหมู่/สาระ")]
        [Required(ErrorMessage = "กรุณาใส่ข้อมูลหมวดหมู่/สาระ")]
        public int m_id { get; set; }
        public string? status{ get; set; }
        public int? c_id{ get; set; }
        public int? g_id{ get; set; }
        public DateTime? lastupdate{ get; set; }

        [ForeignKey("c_id")]
         public virtual Competitionlist? Competitionlist { get; set; }
         [ForeignKey("g_id")]
         public virtual groupreferee? Groupreferee { get;set;}
         [ForeignKey("SettingID")]
         [JsonIgnore]
        public virtual setupsystem? Setupsystem { get; set; }
    }
}