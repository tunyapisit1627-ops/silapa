using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class Certificate
    {
        [Key]
        public int CertificateID { get; set; }
        public int SettingID { get; set; }
        public int RegistrationID { get; set; }
        public int RegistrationNo { get; set; }
        public string AwardID { get; set; }
        public string AwardName { get; set; }
        public string Description { get; set; }
        public string type { get; set; }
        public string CertificateNumber { get; set; } // ฟิลด์ใหม่
        public DateTime IssueDate { get; set; }
        public DateTime lastupdate { get; set; }
    }
}