namespace Silapa.Models
{
    public class StudentViewModel
    {
        public int No { get; set; }
        public int Id { get; set; } // ID จากตาราง Registerdetail
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // (เพิ่มฟิลด์อื่นๆ ที่จำเป็น เช่น ImageUrl)
    }
}