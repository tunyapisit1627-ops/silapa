using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class Racelocation
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public ICollection<racedetails>? racedetails { get; set; }
    }
}