namespace Silapa.Models
{
    public class ScoreViewModel
    {
        public int Id { get; set; }  // ID ของนักเรียนหรือรายการที่ต้องการบันทึกคะแนน
        public decimal Score { get; set; }  // คะแนนที่กรอกโดยผู้ใช้
    }
}