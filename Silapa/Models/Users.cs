using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class Users
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Username{get;set;}
        
    }
}