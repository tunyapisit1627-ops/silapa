using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class Registerhead
    {
        [Key]
        public int id { get; set; }
        public int SettingID{ get; set; }
        public int s_id { get; set; }
        public int c_id { get; set; }
        [DecimalRange(-1, 100, ErrorMessage = "คะแนนต้องอยู่ระหว่าง -1 ถึง 100 เท่านั้น.")]
        public decimal score { get; set; }
        public string status { get; set; }
        public string u_id { get; set; }
        public DateTime lastupdate { get; set; }

        // เพิ่มฟิลด์สำหรับการประกาศผล
        public int? rank { get; set; } // ใช้เพื่อเก็บอันดับ
        public string? award { get; set; } // ใช้เพื่อเก็บรางวัล (เช่น เหรียญทอง, รองชนะเลิศ, เข้าร่วม)



        [NotMapped]
        public IFormFile PdfUpload { get; set; }

        public ICollection<Registerdetail> Registerdetail { get; set; }
        [ForeignKey("s_id")]
        public virtual school School { get; set; }
        [ForeignKey("c_id")]
        public virtual Competitionlist? Competitionlist { get; set; }
        [ForeignKey("SettingID")]
        public virtual setupsystem? Setupsystem { get; set; }

    }
    public class DecimalRangeAttribute : ValidationAttribute
    {
        private readonly decimal _min;
        private readonly decimal _max;

        public DecimalRangeAttribute(double min, double max)
        {
            _min = (decimal)min;
            _max = (decimal)max;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue < _min || decimalValue > _max)
                {
                    return new ValidationResult(ErrorMessage ?? $"ค่า {value} ต้องอยู่ระหว่าง {_min} ถึง {_max}");
                }
            }

            return ValidationResult.Success;
        }
    }
    public class AddTeamViewModel
    {
        public int schoolId { get; set; }
        public int c_id { get; set; }
    }
    public class RegisterViewModel2
    {
        public int Id { get; set; }
        public int SettingID{ get; set; }
        public string SchoolName { get; set; }
        public string CompetitionName { get; set; }
        public string Award{get;set;}
        public string? Rank{get;set;}
        public List<PersonViewModel2> Students { get; set; } = new List<PersonViewModel2>();
        public List<PersonViewModel2> Teachers { get; set; } = new List<PersonViewModel2>();
    }

    public class PersonViewModel2
    {
        public int No { get; set; }
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}