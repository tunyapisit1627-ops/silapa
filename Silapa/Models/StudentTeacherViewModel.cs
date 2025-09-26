namespace Silapa.Models
{
    public class StudentTeacherViewModel
    {
        public List<PersonViewModel> Students { get; set; }
        public List<PersonViewModel> Teachers { get; set; }
    }
    public class PersonViewModel
    {
        public int Id { get; set; }
        public int s_id { get; set; }
        public int c_id { get; set; }

        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
         public string ?ImageUrl{ get; set; }
    }
}