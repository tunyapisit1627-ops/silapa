using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class grouplist
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<school> school { get; set;}
    }
}