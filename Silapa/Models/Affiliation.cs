using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class Affiliation
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<school> schools{ get; set; }
    }
}