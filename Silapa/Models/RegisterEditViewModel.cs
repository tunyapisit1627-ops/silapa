using System.ComponentModel.DataAnnotations;

namespace  Silapa.Models
{
  public class RegisterEditViewModel
{
    // เราต้องมี Id เสมอ เพื่อรู้ว่ากำลังแก้ใคร
    public string Id { get; set; }

    [Required]
    public string titlename { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public string? tel { get; set; }

    // ------------------------------------------
    // ฟิลด์สำหรับเปลี่ยนรหัสผ่าน (ไม่ Required)
    // ------------------------------------------
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string? ConfirmNewPassword { get; set; }

    // ------------------------------------------
    // ฟิลด์สำหรับ Admin (ที่ใช้ใน View)
    // ------------------------------------------
    [Display(Name = "จัดการกลุ่มรายการแข่งขัน")]
    public List<int>? m_id { get; set; }

    // (จำเป็น) ต้องใช้ใน View เพื่อเช็ก style="..."
    public string? a_id { get; set; } 
}  
}