using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Microsoft.AspNetCore.Identity;

namespace Silapa.Models
{
    public class RegisterViewModel : IdentityUser
    {
       // public string Id { get; set; }
        [Required]
        [EmailAddress]
        public string ?Email { get; set; }

        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }
        [Required]
        public string titlename { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        //[Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        //[Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string ?tel { get; set; }
        [Display(Name = "จัดการกลุ่มรายการแข่งขัน")]
        public List<int>? m_id { get; set; }
        [Display(Name = "กลุ่มผู้ใช้งานระบบ")]
        public string? a_id { get; set; }
        public int ?s_id { get; set; }
       public IList<string> ?Roles { get; set; } // เพิ่มเพื่อเก็บข้อมูลบทบาท
    }
}