using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class AspNetUsers
    {
        [Key]
        public int Id { get; set;}
        public string UserName { get; set;}
        public string Email { get; set;}
        public string titlename { get; set;}
        public string FirstName { get; set;}
        public string LastName { get; set;} 
    }
}