using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class AuditLog
{
    [Key]
    public int Id { get; set; }
    
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } // (Id ของคนที่ Login)
    public string SchoolName { get; set; } // (ชื่อโรงเรียนที่สมัคร)
    public string Action { get; set; } // (เช่น "RegisterTeam", "EditTeam")
    public bool Success { get; set; } // (สำเร็จ หรือ ล้มเหลว)
    
    [Column(TypeName = "TEXT")]
    public string? Message { get; set; } // (เก็บ Error Message ถ้าล้มเหลว)
    
    [Column(TypeName = "TEXT")]
    public string? DataPayload { get; set; } // (Optional: เก็บ Json ข้อมูลที่ส่งมา)
}
}