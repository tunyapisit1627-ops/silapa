using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class criterion
    {
        [Key]
        public int id { get; set; }
        public int g_id { get; set; }
        public string name { get; set;}
        public string file { get; set;}
        public string status { get; set;}
    }
}