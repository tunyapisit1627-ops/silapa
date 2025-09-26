
using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class uploadfilepdf
    
    {
        [Key]
        public int id { get; set; }
        public int c_id { get; set; }
        public int s_id { get; set; }
        public string filename { get; set;}
        public string msg { get; set;}
        public string status { get; set; }
        public DateTime lastupdate{ get; set; }

    }
}