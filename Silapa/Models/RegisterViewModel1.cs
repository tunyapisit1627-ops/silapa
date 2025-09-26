namespace Silapa.Models
{
    public class RegisterViewModel1
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string titlename { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
         public string Tel { get; set; }
        public IList<string> Roles { get; set; } // เพิ่มเพื่อเก็บข้อมูลบทบาท
    }
}